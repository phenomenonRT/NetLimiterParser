using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace NetLimiterParser
{
    internal sealed class ProgramExportOptions
    {
        public bool IncludeIps { get; set; }
        public bool IncludeDomains { get; set; }
        public bool TxtFormat { get; set; }
        public int Ipv4Prefix { get; set; }
        public bool IncludeSubdomains { get; set; }
        public bool RemoveDuplicates { get; set; }
        public bool MergeSubnets { get; set; }

        public ProgramExportOptions()
        {
            IncludeIps = true;
            IncludeDomains = true;
            TxtFormat = true;
            Ipv4Prefix = -1;
            IncludeSubdomains = true;
            RemoveDuplicates = true;
            MergeSubnets = true;
        }
    }

    internal static class ProgramListExporter
    {
        private static readonly HashSet<string> KnownSecondLevelSuffixes =
            new HashSet<string>(new string[]
            {
                "ac.uk", "co.uk", "gov.uk", "ltd.uk", "me.uk", "net.uk", "org.uk", "plc.uk",
                "com.au", "edu.au", "gov.au", "id.au", "net.au", "org.au",
                "co.nz", "net.nz", "org.nz",
                "ac.jp", "co.jp", "go.jp", "ne.jp", "or.jp",
                "com.br", "net.br", "org.br",
                "com.cn", "gov.cn", "net.cn", "org.cn",
                "com.hk", "net.hk", "org.hk",
                "co.in", "co.il", "co.kr", "co.za",
                "com.ar", "com.eg", "com.mx", "com.my", "com.ph", "com.pk",
                "com.pl", "com.sa", "com.sg", "com.tr", "com.tw", "com.ua",
                "com.ae", "com.vn"
            }, StringComparer.OrdinalIgnoreCase);

        public static List<string> Build(List<DomainRow> rows, ProgramExportOptions options)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            if (options == null) throw new ArgumentNullException("options");

            List<string> ips = new List<string>();
            List<string> domains = new List<string>();
            List<Ipv4Cidr> cidrs = new List<Ipv4Cidr>();
            HashSet<string> seenIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> seenDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool mergeIpv4 = options.IncludeIps && options.MergeSubnets &&
                options.Ipv4Prefix >= 0 && options.Ipv4Prefix <= 32;

            foreach (DomainRow row in rows)
            {
                if (options.IncludeIps)
                {
                    Ipv4Cidr cidr;
                    if (mergeIpv4 && TryGetIpv4Cidr(row.Ip, options.Ipv4Prefix, out cidr))
                    {
                        cidrs.Add(cidr);
                    }
                    else
                    {
                        string ip = FormatIp(row.Ip, options.Ipv4Prefix);
                        if (ip.Length > 0 && (!options.RemoveDuplicates || seenIps.Add(ip)))
                            ips.Add(ip);
                    }
                }

                if (options.IncludeDomains)
                {
                    string domain = NormalizeDomain(row.Domain);
                    if (domain.Length == 0) continue;

                    if (!options.IncludeSubdomains)
                        domain = GetBaseDomain(domain);
                    if (domain.Length == 0) continue;

                    if (!options.RemoveDuplicates || seenDomains.Add(domain))
                        domains.Add(domain);
                }
            }

            if (cidrs.Count > 0)
            {
                List<Ipv4Cidr> merged = MergeIpv4Cidrs(cidrs);
                foreach (Ipv4Cidr cidr in merged)
                    ips.Add(FormatIpv4Cidr(cidr));
            }

            ips.Sort(CompareIpValues);
            domains.Sort(StringComparer.OrdinalIgnoreCase);

            List<string> result = new List<string>(ips.Count + domains.Count);
            result.AddRange(ips);
            result.AddRange(domains);
            return result;
        }

        public static void Write(List<string> values, string path, bool csv)
        {
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                if (csv) writer.WriteLine("Значение");
                foreach (string value in values)
                    writer.WriteLine(value ?? "");
            }
        }

        public static string GetBaseDomain(string domain)
        {
            string value = NormalizeDomain(domain);
            if (value.Length == 0) return "";

            IPAddress unused;
            if (IPAddress.TryParse(value, out unused)) return value;

            string[] parts = value.Split('.');
            if (parts.Length <= 2) return value;

            string lastTwo = parts[parts.Length - 2] + "." + parts[parts.Length - 1];
            int take = KnownSecondLevelSuffixes.Contains(lastTwo) ? 3 : 2;
            if (take > parts.Length) take = parts.Length;

            StringBuilder result = new StringBuilder();
            for (int i = parts.Length - take; i < parts.Length; i++)
            {
                if (result.Length > 0) result.Append('.');
                result.Append(parts[i]);
            }
            return result.ToString();
        }

        private static string NormalizeDomain(string domain)
        {
            string value = (domain ?? "").Trim();
            while (value.EndsWith(".", StringComparison.Ordinal))
                value = value.Substring(0, value.Length - 1);
            return value.ToLowerInvariant();
        }

        private static string FormatIp(string ip, int prefix)
        {
            string value = (ip ?? "").Trim();
            if (value.Length == 0) return "";
            if (prefix < 0 || prefix > 32) return value;

            IPAddress address;
            if (!IPAddress.TryParse(value, out address)) return value;
            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length != 4) return value;

            uint raw = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                       ((uint)bytes[2] << 8) | bytes[3];
            uint mask = prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);
            uint network = raw & mask;

            byte[] networkBytes = new byte[4];
            networkBytes[0] = (byte)(network >> 24);
            networkBytes[1] = (byte)(network >> 16);
            networkBytes[2] = (byte)(network >> 8);
            networkBytes[3] = (byte)network;

            return new IPAddress(networkBytes).ToString() + "/" +
                prefix.ToString(CultureInfo.InvariantCulture);
        }

        private static bool TryGetIpv4Cidr(string ip, int prefix, out Ipv4Cidr cidr)
        {
            cidr = new Ipv4Cidr();
            string value = (ip ?? "").Trim();
            if (value.Length == 0) return false;

            IPAddress address;
            if (!IPAddress.TryParse(value, out address)) return false;
            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length != 4) return false;

            uint raw = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                       ((uint)bytes[2] << 8) | bytes[3];
            uint mask = prefix == 0 ? 0u : 0xFFFFFFFFu << (32 - prefix);

            cidr.Network = raw & mask;
            cidr.Prefix = prefix;
            return true;
        }

        private static string FormatIpv4Cidr(Ipv4Cidr cidr)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(cidr.Network >> 24);
            bytes[1] = (byte)(cidr.Network >> 16);
            bytes[2] = (byte)(cidr.Network >> 8);
            bytes[3] = (byte)cidr.Network;

            return new IPAddress(bytes).ToString() + "/" +
                cidr.Prefix.ToString(CultureInfo.InvariantCulture);
        }

        private static List<Ipv4Cidr> MergeIpv4Cidrs(List<Ipv4Cidr> source)
        {
            List<Ipv4Cidr> sorted = new List<Ipv4Cidr>(source);
            sorted.Sort(CompareCidrs);

            List<Ipv4Cidr> result = new List<Ipv4Cidr>(sorted.Count);
            for (int i = 0; i < sorted.Count; i++)
            {
                result.Add(sorted[i]);

                while (result.Count >= 2)
                {
                    int lastIndex = result.Count - 1;
                    Ipv4Cidr b = result[lastIndex];
                    Ipv4Cidr a = result[lastIndex - 1];

                    if (a.Prefix <= b.Prefix && ContainsCidr(a, b))
                    {
                        result.RemoveAt(lastIndex);
                        continue;
                    }

                    if (a.Prefix == b.Prefix && a.Prefix > 0 &&
                        IsCidrAligned(a.Network, a.Prefix - 1) &&
                        (ulong)a.Network + BlockSize(a.Prefix) == (ulong)b.Network)
                    {
                        Ipv4Cidr combined = new Ipv4Cidr();
                        combined.Network = a.Network;
                        combined.Prefix = a.Prefix - 1;
                        result[lastIndex - 1] = combined;
                        result.RemoveAt(lastIndex);
                        continue;
                    }

                    break;
                }
            }

            return result;
        }

        private static int CompareCidrs(Ipv4Cidr a, Ipv4Cidr b)
        {
            int cmp = a.Network.CompareTo(b.Network);
            if (cmp != 0) return cmp;
            return a.Prefix.CompareTo(b.Prefix);
        }

        private static bool ContainsCidr(Ipv4Cidr outer, Ipv4Cidr inner)
        {
            if (outer.Prefix > inner.Prefix) return false;

            ulong start = outer.Network;
            ulong end = start + BlockSize(outer.Prefix) - 1;
            ulong value = inner.Network;
            return value >= start && value <= end;
        }

        private static bool IsCidrAligned(uint network, int prefix)
        {
            return (ulong)network % BlockSize(prefix) == 0;
        }

        private static ulong BlockSize(int prefix)
        {
            if (prefix <= 0) return 4294967296UL;
            return 1UL << (32 - prefix);
        }

        private static int CompareIpValues(string a, string b)
        {
            string ipA;
            string ipB;
            int prefixA;
            int prefixB;
            SplitIpValue(a, out ipA, out prefixA);
            SplitIpValue(b, out ipB, out prefixB);

            IPAddress addressA;
            IPAddress addressB;
            bool hasA = IPAddress.TryParse(ipA, out addressA);
            bool hasB = IPAddress.TryParse(ipB, out addressB);

            if (hasA && hasB)
            {
                byte[] bytesA = addressA.GetAddressBytes();
                byte[] bytesB = addressB.GetAddressBytes();
                if (bytesA.Length != bytesB.Length)
                    return bytesA.Length.CompareTo(bytesB.Length);

                for (int i = 0; i < bytesA.Length; i++)
                {
                    int cmp = bytesA[i].CompareTo(bytesB[i]);
                    if (cmp != 0) return cmp;
                }

                return prefixA.CompareTo(prefixB);
            }

            if (hasA) return -1;
            if (hasB) return 1;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private static void SplitIpValue(string value, out string ip, out int prefix)
        {
            ip = value ?? "";
            prefix = -1;
            int slash = ip.IndexOf('/');
            if (slash < 0) return;

            string tail = ip.Substring(slash + 1);
            ip = ip.Substring(0, slash);
            int parsed;
            if (int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                prefix = parsed;
        }

        private struct Ipv4Cidr
        {
            public uint Network;
            public int Prefix;
        }
    }
}
