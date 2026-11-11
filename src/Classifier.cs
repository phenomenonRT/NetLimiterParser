using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace NetLimiterParser
{
    internal sealed class CategoryRule
    {
        public string Type { get; set; }
        public string Pattern { get; set; }
        public string Note { get; set; }

        public CategoryRule()
        {
            Type = "process";
            Pattern = "";
            Note = "";
        }

        public CategoryRule(string type, string pattern, string note)
        {
            Type = type;
            Pattern = pattern;
            Note = note ?? "";
        }

        public string TypeDisplay
        {
            get
            {
                switch ((Type ?? "").ToLowerInvariant())
                {
                    case "process": return "Процесс";
                    case "domain": return "Домен";
                    case "ip": return "IP / подсеть";
                    case "port": return "Порт";
                    default: return Type ?? "";
                }
            }
        }
    }

    internal sealed class CategoryDef
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public bool Enabled { get; set; }
        public List<CategoryRule> Rules { get; set; }

        public CategoryDef()
        {
            Name = "";
            Color = "#94A3B8";
            Enabled = true;
            Rules = new List<CategoryRule>();
        }

        public CategoryDef(string name, string color)
        {
            Name = name;
            Color = color;
            Enabled = true;
            Rules = new List<CategoryRule>();
        }

        public Color DisplayColor
        {
            get { return Theme.FromHex(Color); }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class CategoryFile
    {
        public int Version { get; set; }
        public List<CategoryDef> Categories { get; set; }

        public CategoryFile()
        {
            Version = 1;
            Categories = new List<CategoryDef>();
        }
    }

    internal static class CategoryStore
    {
        public static string LastWarning { get; private set; }

        public static List<CategoryDef> LoadOrCreate()
        {
            LastWarning = null;

            try
            {
                AppPaths.EnsureDataDir();

                if (!File.Exists(AppPaths.CategoriesFile))
                {
                    List<CategoryDef> defaults = CategoryPresets.CreateDefaults();
                    Save(defaults);
                    return defaults;
                }

                string json = File.ReadAllText(AppPaths.CategoriesFile, Encoding.UTF8);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                CategoryFile file = ser.Deserialize<CategoryFile>(json);
                if (file == null || file.Categories == null || file.Categories.Count == 0)
                {
                    LastWarning = "Файл категорий пуст — загружены заготовки.";
                    List<CategoryDef> defaults = CategoryPresets.CreateDefaults();
                    Save(defaults);
                    return defaults;
                }

                bool repaired = false;
                List<CategoryDef> list = Normalize(file.Categories, out repaired);
                if (list.Count == 0)
                {
                    LastWarning = "В файле категорий не найдено корректных записей — загружены заготовки.";
                    List<CategoryDef> defaults = CategoryPresets.CreateDefaults();
                    Save(defaults);
                    return defaults;
                }

                bool hasOther = false;
                foreach (CategoryDef c in list)
                {
                    if (string.Equals(c.Name, "Другое", StringComparison.OrdinalIgnoreCase))
                    {
                        hasOther = true;
                        break;
                    }
                }
                if (!hasOther)
                {
                    list.Add(new CategoryDef("Другое", "#94A3B8"));
                    repaired = true;
                }

                if (file.Version < CategoryPresets.CurrentVersion)
                {
                    CategoryPresets.AddExpandedRules(list);
                    repaired = true;
                }

                if (repaired)
                    Save(list);

                return list;
            }
            catch (Exception ex)
            {
                Log.Error("Ошибка чтения categories.json — используются заготовки.", ex);
                LastWarning = "Не удалось прочитать categories.json — загружены заготовки.";
                try
                {
                    if (File.Exists(AppPaths.CategoriesFile))
                    {
                        string bad = AppPaths.CategoriesFile + ".bad";
                        if (File.Exists(bad)) File.Delete(bad);
                        File.Copy(AppPaths.CategoriesFile, bad, true);
                    }
                }
                catch
                {
                }

                List<CategoryDef> defaults = CategoryPresets.CreateDefaults();
                try { Save(defaults); }
                catch { }
                return defaults;
            }
        }

        private static List<CategoryDef> Normalize(List<CategoryDef> source, out bool repaired)
        {
            repaired = false;
            List<CategoryDef> result = new List<CategoryDef>();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (CategoryDef src in source)
            {
                if (src == null) { repaired = true; continue; }

                string name = (src.Name ?? "").Trim();
                if (name.Length == 0) { repaired = true; continue; }

                string unique = name;
                int suffix = 2;
                while (names.Contains(unique))
                {
                    unique = name + " (" + suffix.ToString(CultureInfo.InvariantCulture) + ")";
                    suffix++;
                    repaired = true;
                }
                names.Add(unique);

                CategoryDef dst = new CategoryDef();
                dst.Name = unique;
                dst.Color = string.IsNullOrWhiteSpace(src.Color) ? "#94A3B8" : src.Color.Trim();
                dst.Enabled = src.Enabled;

                if (src.Rules != null)
                {
                    foreach (CategoryRule r in src.Rules)
                    {
                        if (r == null) { repaired = true; continue; }
                        string type = (r.Type ?? "").Trim().ToLowerInvariant();
                        string pattern = (r.Pattern ?? "").Trim();
                        if (pattern.Length == 0) { repaired = true; continue; }

                        if (type != "process" && type != "domain" && type != "ip" && type != "port")
                        {
                            type = "process";
                            repaired = true;
                        }

                        dst.Rules.Add(new CategoryRule(type, pattern, (r.Note ?? "").Trim()));
                    }
                }

                result.Add(dst);
            }

            return result;
        }

        public static void Save(List<CategoryDef> categories)
        {
            try
            {
                AppPaths.EnsureDataDir();
                string json = ToJson(categories);
                File.WriteAllText(AppPaths.CategoriesFile, json, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log.Error("Не удалось сохранить categories.json.", ex);
                throw;
            }
        }

        public static string ToJson(List<CategoryDef> categories)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  \"version\": ");
            sb.Append(CategoryPresets.CurrentVersion.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(",");
            sb.AppendLine("  \"categories\": [");

            for (int i = 0; i < categories.Count; i++)
            {
                CategoryDef c = categories[i];
                sb.AppendLine("    {");
                sb.Append("      \"name\": ");
                WriteJsonString(sb, c.Name);
                sb.AppendLine(",");
                sb.Append("      \"color\": ");
                WriteJsonString(sb, c.Color);
                sb.AppendLine(",");
                sb.Append("      \"enabled\": ");
                sb.Append(c.Enabled ? "true" : "false");
                sb.AppendLine(",");
                sb.AppendLine("      \"rules\": [");

                for (int j = 0; j < c.Rules.Count; j++)
                {
                    CategoryRule r = c.Rules[j];
                    sb.AppendLine("        {");
                    sb.Append("          \"type\": ");
                    WriteJsonString(sb, r.Type);
                    sb.AppendLine(",");
                    sb.Append("          \"pattern\": ");
                    WriteJsonString(sb, r.Pattern);
                    if (!string.IsNullOrWhiteSpace(r.Note))
                    {
                        sb.AppendLine(",");
                        sb.Append("          \"note\": ");
                        WriteJsonString(sb, r.Note);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                    sb.Append("        }");
                    if (j < c.Rules.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }

                sb.AppendLine("      ]");
                sb.Append("    }");
                if (i < categories.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void WriteJsonString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (value != null)
            {
                foreach (char ch in value)
                {
                    switch (ch)
                    {
                        case '\\': sb.Append("\\\\"); break;
                        case '"': sb.Append("\\\""); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (ch < ' ')
                                sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                sb.Append(ch);
                            break;
                    }
                }
            }
            sb.Append('"');
        }
    }

    internal sealed class CategoryMatch
    {
        public CategoryDef Category { get; set; }
        public CategoryRule Rule { get; set; }
        public int Score { get; set; }
        public bool IsFallback { get; set; }
    }

    internal sealed class Classifier
    {
        private enum RuleKind
        {
            Invalid,
            Process,
            Domain,
            Ip,
            Port
        }

        private sealed class CompiledRule
        {
            public CategoryRule Source;
            public RuleKind Kind;
            public bool Valid;
            public bool HasWildcard;
            public bool ContainsSeparator;
            public string PatternLower;
            public Regex PatternRegex;
            public IpNetwork Network;
            public PortSpec Port;
            public int LiteralCount;
        }

        private sealed class CompiledCategory
        {
            public CategoryDef Definition;
            public int Order;
            public List<CompiledRule> Rules;
        }

        private sealed class CompiledState
        {
            public List<CompiledCategory> Categories;
            public Dictionary<string, CategoryDef> ByName;
            public CategoryDef Fallback;
            public Dictionary<string, CategoryDef> Cache;
            public object CacheLock;

            public CompiledState()
            {
                Categories = new List<CompiledCategory>();
                ByName = new Dictionary<string, CategoryDef>(StringComparer.OrdinalIgnoreCase);
                Cache = new Dictionary<string, CategoryDef>(StringComparer.Ordinal);
                CacheLock = new object();
            }
        }

        private sealed class IpNetwork
        {
            public byte[] Network;
            public int Prefix;
            public AddressFamily Family;

            public bool Contains(IPAddress address)
            {
                if (address == null) return false;
                if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
                    address = address.MapToIPv4();
                if (address.AddressFamily != Family) return false;

                byte[] bytes = address.GetAddressBytes();
                if (bytes.Length != Network.Length) return false;

                int fullBytes = Prefix / 8;
                int remainder = Prefix % 8;

                for (int i = 0; i < fullBytes; i++)
                {
                    if (bytes[i] != Network[i]) return false;
                }

                if (remainder > 0)
                {
                    int mask = 0xFF << (8 - remainder);
                    if ((bytes[fullBytes] & mask) != (Network[fullBytes] & mask)) return false;
                }

                return true;
            }

            public static bool TryParse(string text, out IpNetwork result)
            {
                result = null;
                if (string.IsNullOrWhiteSpace(text)) return false;

                string value = text.Trim();
                string addressPart = value;
                string prefixPart = null;
                int slash = value.IndexOf('/');
                if (slash >= 0)
                {
                    addressPart = value.Substring(0, slash).Trim();
                    prefixPart = value.Substring(slash + 1).Trim();
                }

                IPAddress address;
                if (!IPAddress.TryParse(addressPart, out address)) return false;

                if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
                    address = address.MapToIPv4();

                int maxPrefix = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
                int prefix = maxPrefix;
                if (prefixPart != null)
                {
                    if (!int.TryParse(prefixPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out prefix))
                        return false;
                    if (prefix < 0 || prefix > maxPrefix) return false;
                }

                byte[] bytes = address.GetAddressBytes();
                int fullBytes = prefix / 8;
                int remainder = prefix % 8;

                for (int i = fullBytes + (remainder > 0 ? 1 : 0); i < bytes.Length; i++)
                    bytes[i] = 0;
                if (remainder > 0)
                    bytes[fullBytes] = (byte)(bytes[fullBytes] & (0xFF << (8 - remainder)));

                IpNetwork net = new IpNetwork();
                net.Network = bytes;
                net.Prefix = prefix;
                net.Family = address.AddressFamily;
                result = net;
                return true;
            }
        }

        private sealed class PortSpec
        {
            public int? Protocol;
            public int From;
            public int To;

            public bool Matches(int protocol, int remotePort, int localPort)
            {
                if (Protocol.HasValue && Protocol.Value != protocol) return false;
                int port = remotePort > 0 ? remotePort : localPort;
                if (port <= 0) return false;
                return port >= From && port <= To;
            }

            public static bool TryParse(string text, out PortSpec result)
            {
                result = null;
                if (string.IsNullOrWhiteSpace(text)) return false;

                string value = text.Trim().ToLowerInvariant();
                int? protocol = null;

                if (value.StartsWith("tcp:", StringComparison.Ordinal))
                {
                    protocol = 6;
                    value = value.Substring(4).Trim();
                }
                else if (value.StartsWith("udp:", StringComparison.Ordinal))
                {
                    protocol = 17;
                    value = value.Substring(4).Trim();
                }
                else if (value.StartsWith("icmpv6:", StringComparison.Ordinal))
                {
                    protocol = 58;
                    value = value.Substring(7).Trim();
                }
                else if (value.StartsWith("icmp:", StringComparison.Ordinal))
                {
                    protocol = 1;
                    value = value.Substring(5).Trim();
                }

                int from;
                int to;
                int dash = value.IndexOf('-');
                if (dash >= 0)
                {
                    string a = value.Substring(0, dash).Trim();
                    string b = value.Substring(dash + 1).Trim();
                    if (!int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out from)) return false;
                    if (!int.TryParse(b, NumberStyles.Integer, CultureInfo.InvariantCulture, out to)) return false;
                }
                else
                {
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out from)) return false;
                    to = from;
                }

                if (from < 0 || from > 65535 || to < 0 || to > 65535) return false;
                if (from > to)
                {
                    int tmp = from;
                    from = to;
                    to = tmp;
                }

                PortSpec spec = new PortSpec();
                spec.Protocol = protocol;
                spec.From = from;
                spec.To = to;
                result = spec;
                return true;
            }
        }

        private static readonly Regex WildcardChars = new Regex("[*?]", RegexOptions.Compiled);

        private readonly List<CategoryDef> _sourceCategories;
        private CompiledState _state;

        public Classifier(List<CategoryDef> categories)
        {
            _sourceCategories = categories ?? new List<CategoryDef>();
            Reload();
        }

        public List<CategoryDef> Categories
        {
            get { return _sourceCategories; }
        }

        public void Reload()
        {
            CompiledState state = new CompiledState();

            for (int i = 0; i < _sourceCategories.Count; i++)
            {
                CategoryDef source = _sourceCategories[i];
                if (source == null || string.IsNullOrWhiteSpace(source.Name))
                    continue;

                CategoryDef clone = new CategoryDef(source.Name.Trim(), source.Color);
                clone.Enabled = source.Enabled;

                CompiledCategory cc = new CompiledCategory();
                cc.Definition = clone;
                cc.Order = i;
                cc.Rules = new List<CompiledRule>();

                if (source.Rules != null)
                {
                    foreach (CategoryRule rule in source.Rules)
                    {
                        if (rule == null) continue;
                        clone.Rules.Add(new CategoryRule(rule.Type, rule.Pattern, rule.Note));
                        CompiledRule compiled = CompileRule(rule);
                        if (compiled.Valid)
                            cc.Rules.Add(compiled);
                    }
                }

                state.Categories.Add(cc);
                if (!state.ByName.ContainsKey(clone.Name))
                    state.ByName[clone.Name] = clone;
            }

            CategoryDef fallback;
            if (!state.ByName.TryGetValue("Другое", out fallback))
            {
                fallback = new CategoryDef("Другое", "#94A3B8");
                state.ByName[fallback.Name] = fallback;
            }
            state.Fallback = fallback;

            _state = state;
        }

        private static CompiledRule CompileRule(CategoryRule rule)
        {
            CompiledRule cr = new CompiledRule();
            cr.Source = rule;
            cr.Valid = false;
            cr.Kind = RuleKind.Invalid;

            string type = (rule.Type ?? "").Trim().ToLowerInvariant();
            string pattern = (rule.Pattern ?? "").Trim();
            if (pattern.Length == 0) return cr;

            switch (type)
            {
                case "process": cr.Kind = RuleKind.Process; break;
                case "domain": cr.Kind = RuleKind.Domain; break;
                case "ip": cr.Kind = RuleKind.Ip; break;
                case "port": cr.Kind = RuleKind.Port; break;
                default: return cr;
            }

            cr.PatternLower = pattern.ToLowerInvariant();
            cr.ContainsSeparator = pattern.IndexOf('\\') >= 0 || pattern.IndexOf('/') >= 0;
            cr.HasWildcard = pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0;
            cr.LiteralCount = 0;
            foreach (char ch in pattern)
            {
                if (ch != '*' && ch != '?') cr.LiteralCount++;
            }

            if (cr.Kind == RuleKind.Process || cr.Kind == RuleKind.Domain)
            {
                if (cr.HasWildcard)
                {
                    try
                    {
                        string escaped = Regex.Escape(pattern);
                        escaped = escaped.Replace("\\*", ".*").Replace("\\?", ".");
                        cr.PatternRegex = new Regex("^" + escaped + "$",
                            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                            TimeSpan.FromMilliseconds(300));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Некорректный шаблон правила: " + pattern, ex);
                        return cr;
                    }
                }
                cr.Valid = true;
                return cr;
            }

            if (cr.Kind == RuleKind.Ip)
            {
                IpNetwork network;
                if (!IpNetwork.TryParse(pattern, out network))
                    return cr;
                cr.Network = network;
                cr.Valid = true;
                return cr;
            }

            if (cr.Kind == RuleKind.Port)
            {
                PortSpec spec;
                if (!PortSpec.TryParse(pattern, out spec))
                    return cr;
                cr.Port = spec;
                cr.Valid = true;
                return cr;
            }

            return cr;
        }

        public CategoryDef Classify(string appPath, string domain, string ip, int remotePort, int localPort, string protocol)
        {
            return Classify(appPath, domain, ip, remotePort, localPort, protocol, false);
        }

        public CategoryDef ClassifyService(string domain, string ip, int remotePort, string protocol)
        {
            return Classify("", domain, ip, remotePort, 0, protocol, true);
        }

        private CategoryDef Classify(string appPath, string domain, string ip, int remotePort, int localPort, string protocol, bool serviceOnly)
        {
            CompiledState state = _state;
            string key = BuildCacheKey(serviceOnly, appPath, domain, ip, remotePort, localPort, protocol);

            lock (state.CacheLock)
            {
                CategoryDef cached;
                if (state.Cache.TryGetValue(key, out cached))
                    return cached;
            }

            CategoryMatch match = MatchCore(state, appPath, domain, ip, remotePort, localPort, protocol, serviceOnly);
            CategoryDef category = match != null && match.Category != null ? match.Category : state.Fallback;

            lock (state.CacheLock)
            {
                if (state.Cache.Count > 200000) state.Cache.Clear();
                state.Cache[key] = category;
            }

            return category;
        }

        public CategoryMatch Match(string appPath, string domain, string ip, int remotePort, int localPort, string protocol, bool serviceOnly)
        {
            return MatchCore(_state, appPath, domain, ip, remotePort, localPort, protocol, serviceOnly);
        }

        private static string BuildCacheKey(bool serviceOnly, string appPath, string domain, string ip, int remotePort, int localPort, string protocol)
        {
            StringBuilder sb = new StringBuilder(96);
            sb.Append(serviceOnly ? "S|" : "A|");
            sb.Append(appPath ?? "");
            sb.Append('|');
            sb.Append(domain ?? "");
            sb.Append('|');
            sb.Append(ip ?? "");
            sb.Append('|');
            sb.Append(remotePort.ToString(CultureInfo.InvariantCulture));
            sb.Append('|');
            sb.Append(localPort.ToString(CultureInfo.InvariantCulture));
            sb.Append('|');
            sb.Append(protocol ?? "");
            return sb.ToString();
        }

        private static CategoryMatch MatchCore(
            CompiledState state,
            string appPath,
            string domain,
            string ip,
            int remotePort,
            int localPort,
            string protocol,
            bool serviceOnly)
        {
            string path = NormalizePath(appPath);
            string fileName = GetFileName(path);
            string dom = NormalizeDomain(domain);

            IPAddress address = null;
            if (!string.IsNullOrWhiteSpace(ip))
            {
                string ipText = ip.Trim();
                if (ipText.StartsWith("[", StringComparison.Ordinal) && ipText.EndsWith("]", StringComparison.Ordinal))
                    ipText = ipText.Substring(1, ipText.Length - 2);
                IPAddress.TryParse(ipText, out address);
            }

            int proto = ProtocolToNumber(protocol);
            CategoryMatch best = null;

            foreach (CompiledCategory category in state.Categories)
            {
                if (category.Definition == null || !category.Definition.Enabled)
                    continue;

                CategoryRule bestRule = null;
                int bestScore = 0;

                foreach (CompiledRule rule in category.Rules)
                {
                    if (!rule.Valid) continue;
                    if (serviceOnly && rule.Kind == RuleKind.Process) continue;

                    int score = 0;
                    bool matched = false;

                    switch (rule.Kind)
                    {
                        case RuleKind.Process:
                            matched = MatchProcess(rule, path, fileName);
                            if (matched)
                            {
                                if (!rule.HasWildcard)
                                {
                                    score = rule.ContainsSeparator ? 120 : 110;
                                }
                                else
                                {
                                    score = 90 + Math.Min(rule.LiteralCount, 20);
                                }
                            }
                            break;

                        case RuleKind.Domain:
                            matched = MatchDomain(rule, dom);
                            if (matched)
                            {
                                score = rule.HasWildcard
                                    ? 80 + Math.Min(rule.LiteralCount, 10)
                                    : 85;
                            }
                            break;

                        case RuleKind.Ip:
                            matched = address != null && rule.Network != null && rule.Network.Contains(address);
                            if (matched)
                            {
                                score = rule.Network.Prefix >= (rule.Network.Family == AddressFamily.InterNetwork ? 32 : 128)
                                    ? 76
                                    : 72 + Math.Min(rule.Network.Prefix / 8, 6);
                            }
                            break;

                        case RuleKind.Port:
                            matched = rule.Port.Matches(proto, remotePort, localPort);
                            if (matched)
                                score = 60;
                            break;
                    }

                    if (matched && score > bestScore)
                    {
                        bestScore = score;
                        bestRule = rule.Source;
                    }
                }

                if (bestRule != null)
                {
                    if (best == null || bestScore > best.Score ||
                        (bestScore == best.Score && category.Order < IndexOfCategory(state, best.Category)))
                    {
                        best = new CategoryMatch();
                        best.Category = category.Definition;
                        best.Rule = bestRule;
                        best.Score = bestScore;
                        best.IsFallback = false;
                    }
                }
            }

            if (best == null)
            {
                best = new CategoryMatch();
                best.Category = state.Fallback;
                best.Rule = null;
                best.Score = 0;
                best.IsFallback = true;
            }

            return best;
        }

        private static int IndexOfCategory(CompiledState state, CategoryDef category)
        {
            for (int i = 0; i < state.Categories.Count; i++)
            {
                if (ReferenceEquals(state.Categories[i].Definition, category))
                    return state.Categories[i].Order;
            }
            return int.MaxValue;
        }

        private static bool MatchProcess(CompiledRule rule, string path, string fileName)
        {
            if (path.Length == 0 && fileName.Length == 0) return false;
            string target = rule.ContainsSeparator ? path : fileName;
            if (target.Length == 0) target = path;

            if (rule.HasWildcard)
                return rule.PatternRegex != null && rule.PatternRegex.IsMatch(target);

            return string.Equals(target, rule.PatternLower, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchDomain(CompiledRule rule, string domain)
        {
            if (domain.Length == 0) return false;
            if (rule.HasWildcard)
                return rule.PatternRegex != null && rule.PatternRegex.IsMatch(domain);

            if (string.Equals(domain, rule.PatternLower, StringComparison.OrdinalIgnoreCase))
                return true;

            return domain.EndsWith("." + rule.PatternLower, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            return path.Trim().Replace('/', '\\').ToLowerInvariant();
        }

        private static string GetFileName(string normalizedPath)
        {
            if (normalizedPath.Length == 0) return "";
            int i = normalizedPath.LastIndexOf('\\');
            return i >= 0 ? normalizedPath.Substring(i + 1) : normalizedPath;
        }

        private static string NormalizeDomain(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain)) return "";
            return domain.Trim().TrimEnd('.').ToLowerInvariant();
        }

        private static int ProtocolToNumber(string protocol)
        {
            if (string.IsNullOrWhiteSpace(protocol)) return 0;
            string p = protocol.Trim().ToUpperInvariant();
            if (p == "TCP") return 6;
            if (p == "UDP") return 17;
            if (p == "ICMP") return 1;
            if (p == "ICMPV6" || p == "ICMPV6") return 58;
            int n;
            if (int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) return n;
            return 0;
        }

        public CategoryDef GetCategory(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return _state.Fallback;
            CategoryDef def;
            return _state.ByName.TryGetValue(name.Trim(), out def) ? def : _state.Fallback;
        }

        public Color GetColor(string name)
        {
            CategoryDef def = GetCategory(name);
            return def != null ? def.DisplayColor : Color.FromArgb(148, 163, 184);
        }

        public CategoryDef Fallback
        {
            get { return _state.Fallback; }
        }

        public static bool ValidateRule(CategoryRule rule, out string error)
        {
            error = "";
            if (rule == null || string.IsNullOrWhiteSpace(rule.Pattern))
            {
                error = "Укажите шаблон правила.";
                return false;
            }

            string type = (rule.Type ?? "").Trim().ToLowerInvariant();
            if (type == "ip")
            {
                IpNetwork net;
                if (!IpNetwork.TryParse(rule.Pattern, out net))
                {
                    error = "Некорректный IP-адрес или подсеть. Пример: 192.168.0.0/16 или 2001:db8::/32.";
                    return false;
                }
            }
            else if (type == "port")
            {
                PortSpec spec;
                if (!PortSpec.TryParse(rule.Pattern, out spec))
                {
                    error = "Некорректный порт. Пример: 443, 27015-27050 или udp:27015-27050.";
                    return false;
                }
            }
            else if (type != "process" && type != "domain")
            {
                error = "Неизвестный тип правила.";
                return false;
            }

            if (type == "process" && !WildcardChars.IsMatch(rule.Pattern) && rule.Pattern.Trim().Length < 2)
            {
                error = "Слишком короткий шаблон процесса.";
                return false;
            }

            return true;
        }
    }
}
