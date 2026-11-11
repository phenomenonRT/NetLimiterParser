using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace NetLimiterParser
{
    internal sealed class UpdateCheckResult
    {
        public bool Success;
        public bool NoReleases;
        public bool HasUpdate;
        public bool VersionComparisonFailed;
        public string CurrentVersion = "";
        public string LatestVersion = "";
        public string TagName = "";
        public string ReleaseName = "";
        public string ReleaseUrl = "";
        public string ReleaseNotes = "";
        public DateTime PublishedUtc = DateTime.MinValue;
        public string ErrorMessage = "";

        public bool HasPublishedDate
        {
            get { return PublishedUtc != DateTime.MinValue; }
        }
    }

    internal static class UpdateChecker
    {
        public const string RepositoryUrl = "https://github.com/phenomenonRT/NetLimiterParser";
        public const string ReleasesUrl = RepositoryUrl + "/releases";

        private const string LatestReleaseApi =
            "https://api.github.com/repos/phenomenonRT/NetLimiterParser/releases/latest";
        private const int RequestTimeoutMs = 15000;

        public static string CurrentVersion
        {
            get { return FormatVersion(Assembly.GetExecutingAssembly().GetName().Version); }
        }

        public static UpdateCheckResult Check()
        {
            UpdateCheckResult result = new UpdateCheckResult();
            result.CurrentVersion = CurrentVersion;

            try
            {
                EnableModernTls();

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LatestReleaseApi);
                request.Method = "GET";
                request.UserAgent = "NetLimiterParser/" + result.CurrentVersion;
                request.Accept = "application/vnd.github+json";
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Timeout = RequestTimeoutMs;
                request.ReadWriteTimeout = RequestTimeoutMs;
                request.AllowAutoRedirect = true;

                string json;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                return ParseResponse(json);
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null)
                {
                    int statusCode = (int)response.StatusCode;
                    try { response.Dispose(); }
                    catch { }

                    if (statusCode == 404)
                    {
                        result.Success = true;
                        result.NoReleases = true;
                        return result;
                    }

                    if (statusCode == 403)
                    {
                        result.ErrorMessage = "GitHub ограничил частоту запросов (HTTP 403). Попробуйте позже.";
                        return result;
                    }

                    result.ErrorMessage = "GitHub вернул ошибку HTTP " + statusCode.ToString(CultureInfo.InvariantCulture) + ".";
                    return result;
                }

                if (ex.Status == WebExceptionStatus.Timeout)
                    result.ErrorMessage = "Превышено время ожидания ответа GitHub.";
                else if (ex.Status == WebExceptionStatus.NameResolutionFailure ||
                         ex.Status == WebExceptionStatus.ProxyNameResolutionFailure)
                    result.ErrorMessage = "Не удалось найти сервер api.github.com. Проверьте подключение к интернету.";
                else if (ex.Status == WebExceptionStatus.ConnectFailure)
                    result.ErrorMessage = "Не удалось подключиться к GitHub. Проверьте подключение к интернету или прокси.";
                else
                    result.ErrorMessage = "Ошибка сети при обращении к GitHub: " + ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Не удалось проверить обновления: " + ex.Message;
                return result;
            }
        }

        private static void EnableModernTls()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
            }
        }

        internal static UpdateCheckResult ParseResponse(string json)
        {
            UpdateCheckResult result = new UpdateCheckResult();
            result.CurrentVersion = CurrentVersion;

            Dictionary<string, object> release;
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                release = serializer.DeserializeObject(json) as Dictionary<string, object>;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "GitHub вернул ответ в неизвестном формате: " + ex.Message;
                return result;
            }

            if (release == null)
            {
                result.ErrorMessage = "GitHub вернул пустой ответ.";
                return result;
            }

            result.TagName = GetString(release, "tag_name");
            result.ReleaseName = GetString(release, "name");
            result.ReleaseUrl = GetString(release, "html_url");
            result.ReleaseNotes = GetString(release, "body");
            result.PublishedUtc = ParseDate(GetString(release, "published_at"));

            if (string.IsNullOrWhiteSpace(result.TagName))
            {
                result.ErrorMessage = "В ответе GitHub нет номера версии релиза (tag_name).";
                return result;
            }

            result.LatestVersion = NormalizeTag(result.TagName);

            Version latest;
            if (!TryParseVersion(result.TagName, out latest))
            {
                result.VersionComparisonFailed = true;
                result.Success = true;
                result.HasUpdate = false;
                return result;
            }

            Version current = Assembly.GetExecutingAssembly().GetName().Version;
            result.HasUpdate = NormalizeVersion(current).CompareTo(NormalizeVersion(latest)) < 0;
            result.Success = true;
            return result;
        }

        private static string GetString(Dictionary<string, object> map, string key)
        {
            object value;
            if (map != null && map.TryGetValue(key, out value) && value != null)
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            return "";
        }

        private static DateTime ParseDate(string text)
        {
            DateTime value;
            if (DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out value))
            {
                return value;
            }
            return DateTime.MinValue;
        }

        private static string NormalizeTag(string tag)
        {
            string text = (tag ?? "").Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(1).Trim();
            return text;
        }

        private static bool TryParseVersion(string tag, out Version version)
        {
            version = null;
            string text = NormalizeTag(tag);
            if (text.Length == 0) return false;

            int end = 0;
            while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.'))
                end++;

            text = text.Substring(0, end).TrimEnd('.');
            if (text.Length == 0) return false;

            return Version.TryParse(text, out version);
        }

        private static Version NormalizeVersion(Version version)
        {
            if (version == null) return new Version(0, 0, 0, 0);
            int build = version.Build < 0 ? 0 : version.Build;
            int revision = version.Revision < 0 ? 0 : version.Revision;
            return new Version(version.Major, version.Minor, build, revision);
        }

        private static string FormatVersion(Version version)
        {
            if (version == null) return "0.0.0";
            if (version.Revision > 0) return version.ToString(4);
            if (version.Build >= 0) return version.ToString(3);
            return version.ToString(2);
        }
    }
}
