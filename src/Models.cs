using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace NetLimiterParser
{
    internal static class IpTools
    {
        public static string Ipv4(long value)
        {
            uint v = unchecked((uint)value);
            return new IPAddress(new byte[]
            {
                (byte)(v >> 24),
                (byte)(v >> 16),
                (byte)(v >> 8),
                (byte)v
            }).ToString();
        }

        public static string Ipv6(long p1, long p2, long p3, long p4)
        {
            uint[] parts = new uint[]
            {
                unchecked((uint)p1),
                unchecked((uint)p2),
                unchecked((uint)p3),
                unchecked((uint)p4)
            };

            byte[] bytes = new byte[16];
            int k = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                uint p = parts[i];
                bytes[k++] = (byte)(p >> 16);
                bytes[k++] = (byte)(p >> 24);
                bytes[k++] = (byte)p;
                bytes[k++] = (byte)(p >> 8);
            }

            try
            {
                return new IPAddress(bytes).ToString();
            }
            catch
            {
                return "IPv6?";
            }
        }

        public static DateTime FromWinMilliseconds(long milliseconds)
        {
            try
            {
                return DateTime.FromFileTimeUtc(milliseconds * 10000L);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static long ToWinMilliseconds(DateTime localTime)
        {
            DateTime utc = localTime.Kind == DateTimeKind.Utc ? localTime : localTime.ToUniversalTime();
            return utc.ToFileTimeUtc() / 10000L;
        }

        public static DateTime FromUnixSeconds(long seconds)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static long ToUnixSeconds(DateTime localTime)
        {
            DateTime utc = localTime.Kind == DateTimeKind.Utc ? localTime : localTime.ToUniversalTime();
            return new DateTimeOffset(utc).ToUnixTimeSeconds();
        }

        public static string FormatLocal(DateTime utc)
        {
            if (utc == DateTime.MinValue) return "";
            return utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        }
    }

    internal static class DisplayNames
    {
        public static string FromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "(неизвестно)";
            string p = path.TrimEnd('\\', '/');
            if (p.Length == 0) return path;

            int i = p.LastIndexOf('\\');
            int j = p.LastIndexOf('/');
            if (j > i) i = j;
            string name = i >= 0 ? p.Substring(i + 1) : p;
            return string.IsNullOrWhiteSpace(name) ? p : name;
        }
    }

    internal static class Protocols
    {
        public static string Name(int proto)
        {
            switch (proto)
            {
                case 1: return "ICMP";
                case 2: return "IGMP";
                case 4: return "IPIP";
                case 6: return "TCP";
                case 17: return "UDP";
                case 58: return "ICMPv6";
                default: return "IP " + proto.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    internal sealed class DbStats
    {
        public string Path { get; set; }
        public string EffectivePath { get; set; }
        public bool IsSnapshot { get; set; }
        public long FileSize { get; set; }
        public int AppCount { get; set; }
        public int UserCount { get; set; }
        public int V4Count { get; set; }
        public int V6Count { get; set; }
        public long ConnectionCount { get; set; }
        public long TransferCount { get; set; }
        public DateTime DataMinUtc { get; set; }
        public DateTime DataMaxUtc { get; set; }
        public DateTime TransferMaxUtc { get; set; }
    }

    internal sealed class ConnectionRow
    {
        public long RowId { get; set; }
        public long TimeStartMs { get; set; }
        public DateTime TimeUtc { get; set; }
        public double DurationSec { get; set; }
        public int AppId { get; set; }
        public int UserId { get; set; }
        public int Proto { get; set; }
        public string LocalIp { get; set; }
        public int LocalPort { get; set; }
        public string RemoteIp { get; set; }
        public int RemotePort { get; set; }
        public string Domain { get; set; }
        public long InBytes { get; set; }
        public long OutBytes { get; set; }
        public int Flags { get; set; }
        public string AppPath { get; set; }
        public string Category { get; set; }

        public bool IsIpv6
        {
            get { return (Flags & 2) != 0; }
        }

        public ConnectionRow()
        {
            LocalIp = "";
            RemoteIp = "";
            Domain = "";
            AppPath = "";
            Category = "";
        }
    }

    internal sealed class AppAggregate
    {
        public int AppId { get; set; }
        public string AppPath { get; set; }
        public string Category { get; set; }
        public long Connections { get; set; }
        public long InBytes { get; set; }
        public long OutBytes { get; set; }
        public DateTime LastUtc { get; set; }
        public long UniqueRemoteIps { get; set; }

        public AppAggregate()
        {
            AppPath = "";
            Category = "";
        }
    }

    internal sealed class TrafficRow
    {
        public int AppId { get; set; }
        public string AppPath { get; set; }
        public string Category { get; set; }
        public int Proto { get; set; }
        public string LocalIp { get; set; }
        public string RemoteIp { get; set; }
        public int RemotePort { get; set; }
        public string Domain { get; set; }
        public long InBytes { get; set; }
        public long OutBytes { get; set; }
        public long TotalBytes { get; set; }
        public long RecordCount { get; set; }
        public DateTime FirstUtc { get; set; }
        public DateTime LastUtc { get; set; }
        public bool IsIpv6 { get; set; }

        public TrafficRow()
        {
            AppPath = "";
            Category = "";
            LocalIp = "";
            RemoteIp = "";
            Domain = "";
        }
    }

    internal sealed class DomainRow
    {
        public string Ip { get; set; }
        public bool IsIpv6 { get; set; }
        public string Domain { get; set; }
        public string Service { get; set; }
        public string Protocols { get; set; }
        public long Connections { get; set; }
        public long InBytes { get; set; }
        public long OutBytes { get; set; }
        public long TotalBytes { get; set; }
        public DateTime LastUtc { get; set; }

        public DomainRow()
        {
            Ip = "";
            Domain = "";
            Service = "";
            Protocols = "";
        }
    }

    internal sealed class ConnectionsPage
    {
        public List<ConnectionRow> Rows { get; set; }
        public bool HasMore { get; set; }
        public long LastTimeMs { get; set; }
        public long LastRowId { get; set; }

        public ConnectionsPage()
        {
            Rows = new List<ConnectionRow>();
        }
    }

    internal sealed class ConnectionsResult
    {
        public List<ConnectionRow> Rows { get; set; }
        public bool Exhausted { get; set; }
        public long Scanned { get; set; }
        public long Total { get; set; }
        public long CursorTime { get; set; }
        public long CursorRowId { get; set; }
        public bool HasCursor { get; set; }
        public bool ScanLimitHit { get; set; }

        public ConnectionsResult()
        {
            Rows = new List<ConnectionRow>();
        }
    }

    internal sealed class TransferProgress
    {
        public int BlobsDone { get; set; }
        public int BlobsTotal { get; set; }
        public long Records { get; set; }

        public int Percent
        {
            get
            {
                if (BlobsTotal <= 0) return 0;
                int p = (int)((long)BlobsDone * 100L / BlobsTotal);
                if (p < 0) p = 0;
                if (p > 100) p = 100;
                return p;
            }
        }
    }

    internal sealed class TransferSummary
    {
        public List<TrafficRow> Rows { get; set; }
        public long TotalIn { get; set; }
        public long TotalOut { get; set; }
        public long TotalRecords { get; set; }
        public int Blobs { get; set; }
        public long Groups { get; set; }
        public DateTime FirstUtc { get; set; }
        public DateTime LastUtc { get; set; }

        public TransferSummary()
        {
            Rows = new List<TrafficRow>();
        }
    }
}
