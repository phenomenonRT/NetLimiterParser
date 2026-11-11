using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace NetLimiterParser
{
    internal sealed class ConnectionsPageRequest
    {
        public long FromMs { get; set; }
        public long ToMs { get; set; }
        public int Proto { get; set; }
        public int AppId { get; set; }
        public int Limit { get; set; }
        public bool HasCursor { get; set; }
        public long CursorTime { get; set; }
        public long CursorRowId { get; set; }

        public ConnectionsPageRequest()
        {
            Proto = 0;
            AppId = 0;
            Limit = 1000;
        }
    }

    internal sealed class NetLimiterDatabase : IDisposable
    {
        private sealed class Ipv6Info
        {
            public string Ip;
            public string Domain;

            public Ipv6Info(string ip, string domain)
            {
                Ip = ip;
                Domain = domain ?? "";
            }
        }

        private struct TransferGroupKey : IEquatable<TransferGroupKey>
        {
            public int AppId;
            public int RemotePort;
            public long RemoteId;
            public long LocalId;
            public byte Proto;
            public bool Ipv6;

            public bool Equals(TransferGroupKey other)
            {
                return AppId == other.AppId
                    && RemotePort == other.RemotePort
                    && RemoteId == other.RemoteId
                    && LocalId == other.LocalId
                    && Proto == other.Proto
                    && Ipv6 == other.Ipv6;
            }

            public override bool Equals(object obj)
            {
                return obj is TransferGroupKey && Equals((TransferGroupKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + AppId;
                    hash = hash * 31 + RemotePort;
                    hash = hash * 31 + (int)RemoteId;
                    hash = hash * 31 + (int)(RemoteId >> 32);
                    hash = hash * 31 + (int)LocalId;
                    hash = hash * 31 + (int)(LocalId >> 32);
                    hash = hash * 31 + Proto;
                    hash = hash * 31 + (Ipv6 ? 1 : 0);
                    return hash;
                }
            }
        }

        private sealed class TransferBucket
        {
            public int AppId;
            public int RemotePort;
            public int LocalPort;
            public int Proto;
            public bool Ipv6;
            public string AppPath;
            public string LocalIp;
            public string RemoteIp;
            public string Domain;
            public string Category;
            public long InBytes;
            public long OutBytes;
            public long Records;
            public DateTime FirstUtc;
            public DateTime LastUtc;
            public bool HasTime;
        }

        private sealed class DomainAccumulator
        {
            public DomainRow Row;
            public SortedSet<string> Protocols;
            public long BestCategoryBytes;
        }

        private readonly string _sourcePath;
        private readonly string _effectivePath;
        private readonly bool _snapshot;
        private readonly Classifier _classifier;
        private readonly Dictionary<int, string> _apps;
        private readonly Dictionary<int, string> _users;
        private readonly Dictionary<long, string> _v4;
        private readonly Dictionary<long, Ipv6Info> _v6;
        private bool _disposed;

        public string SourcePath
        {
            get { return _sourcePath; }
        }

        public string EffectivePath
        {
            get { return _effectivePath; }
        }

        public bool IsSnapshot
        {
            get { return _snapshot; }
        }

        public Dictionary<int, string> Apps
        {
            get { return _apps; }
        }

        public int AppCount
        {
            get { return _apps.Count; }
        }

        public int UserCount
        {
            get { return _users.Count; }
        }

        public int Ipv4Count
        {
            get { return _v4.Count; }
        }

        public int Ipv6Count
        {
            get { return _v6.Count; }
        }

        public Classifier Classifier
        {
            get { return _classifier; }
        }

        private NetLimiterDatabase(string sourcePath, string effectivePath, bool snapshot, Classifier classifier)
        {
            _sourcePath = sourcePath;
            _effectivePath = effectivePath;
            _snapshot = snapshot;
            _classifier = classifier;
            _apps = new Dictionary<int, string>();
            _users = new Dictionary<int, string>();
            _v4 = new Dictionary<long, string>();
            _v6 = new Dictionary<long, Ipv6Info>();

            using (SqliteConnection connection = OpenConnection())
            {
                ValidateSchema(connection);
                LoadDictionaries(connection);
            }
        }

        public static NetLimiterDatabase Open(string path, Classifier classifier, Action<int, string> status)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Не указан путь к базе данных NetLimiter.", "path");
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл базы данных не найден: " + path, path);

            Exception firstError = null;
            try
            {
                return new NetLimiterDatabase(path, path, false, classifier);
            }
            catch (Exception ex)
            {
                firstError = ex;
                Log.Warn("Не удалось открыть базу напрямую: " + ex.Message);
            }

            try
            {
                if (status != null) status(-1, "Создание снимка базы данных...");
                string snapshot = AppPaths.SnapshotFile;
                CreateSnapshot(path, snapshot, status);

                NetLimiterDatabase database = new NetLimiterDatabase(path, snapshot, true, classifier);
                Log.Info("Открыт снимок базы: " + snapshot);
                return database;
            }
            catch (Exception snapshotError)
            {
                Log.Error("Не удалось создать снимок базы.", snapshotError);
                throw new InvalidOperationException(
                    "Не удалось открыть базу данных NetLimiter." + Environment.NewLine +
                    "Прямое открытие: " + (firstError != null ? firstError.Message : "неизвестная ошибка") + Environment.NewLine +
                    "Снимок: " + snapshotError.Message,
                    snapshotError);
            }
        }

        private static void CreateSnapshot(string source, string destination, Action<int, string> status)
        {
            AppPaths.EnsureDataDir();
            DeleteSnapshotFiles(destination);

            try
            {
                BackupWithSqlite(source, destination, status);
                return;
            }
            catch (Exception ex)
            {
                Log.Warn("Онлайн-снимок через SQLite не удался, копирую файл: " + ex.Message);
                DeleteSnapshotFiles(destination);
            }

            CopyWithSidecars(source, destination, status);
        }

        private static void DeleteSnapshotFiles(string destination)
        {
            DeleteFileSafe(destination);
            DeleteFileSafe(destination + "-wal");
            DeleteFileSafe(destination + "-shm");
            DeleteFileSafe(destination + "-journal");
        }

        private static void DeleteFileSafe(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Warn("Не удалось удалить файл " + path + ": " + ex.Message);
            }
        }

        private static void BackupWithSqlite(string source, string destination, Action<int, string> status)
        {
            if (status != null) status(0, "Создание снимка базы данных...");

            using (SqliteConnection sourceConnection = new SqliteConnection(source))
            {
                IntPtr destinationDb;
                int rc = NativeSqlite.sqlite3_open_v2(
                    destination,
                    out destinationDb,
                    NativeSqlite.SQLITE_OPEN_READWRITE | NativeSqlite.SQLITE_OPEN_CREATE,
                    IntPtr.Zero);

                if (rc != NativeSqlite.SQLITE_OK || destinationDb == IntPtr.Zero)
                {
                    string message = NativeSqlite.ErrorMessage(destinationDb);
                    if (destinationDb != IntPtr.Zero) NativeSqlite.sqlite3_close_v2(destinationDb);
                    throw new SqliteException(rc, "Не удалось создать файл снимка: " + message);
                }

                IntPtr backup = IntPtr.Zero;
                int stepResult = NativeSqlite.SQLITE_OK;
                try
                {
                    backup = NativeSqlite.sqlite3_backup_init(destinationDb, "main", sourceConnection.Handle, "main");
                    if (backup == IntPtr.Zero)
                        throw new SqliteException(rc, "Не удалось запустить копирование базы: " + NativeSqlite.ErrorMessage(destinationDb));

                    int guard = 0;
                    while (true)
                    {
                        stepResult = NativeSqlite.sqlite3_backup_step(backup, 1024);
                        int pageCount = NativeSqlite.sqlite3_backup_pagecount(backup);
                        int remaining = NativeSqlite.sqlite3_backup_remaining(backup);
                        if (status != null && pageCount > 0)
                        {
                            int percent = (int)((long)(pageCount - remaining) * 100L / pageCount);
                            status(percent, "Создание снимка базы данных... " + percent.ToString(CultureInfo.InvariantCulture) + "%");
                        }

                        if (stepResult == NativeSqlite.SQLITE_DONE) break;
                        if (stepResult == NativeSqlite.SQLITE_OK) continue;
                        if (stepResult == NativeSqlite.SQLITE_BUSY || stepResult == NativeSqlite.SQLITE_LOCKED)
                        {
                            Thread.Sleep(50);
                            guard++;
                            if (guard > 400)
                                throw new SqliteException(stepResult, "База данных занята другим процессом.");
                            continue;
                        }
                        break;
                    }
                }
                finally
                {
                    if (backup != IntPtr.Zero)
                        NativeSqlite.sqlite3_backup_finish(backup);
                    NativeSqlite.sqlite3_close_v2(destinationDb);
                }

                if (stepResult != NativeSqlite.SQLITE_DONE)
                    throw new SqliteException(stepResult, "Копирование базы данных не завершено.");

                DeleteFileSafe(destination + "-wal");
                DeleteFileSafe(destination + "-shm");
            }
        }

        private static void CopyWithSidecars(string source, string destination, Action<int, string> status)
        {
            if (status != null) status(-1, "Копирование файла базы данных...");

            FileInfo info = new FileInfo(source);
            long total = info.Length;
            long copied = 0;
            byte[] buffer = new byte[1024 * 1024];

            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                while (true)
                {
                    int read = input.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;
                    output.Write(buffer, 0, read);
                    copied += read;
                    if (status != null && total > 0)
                    {
                        int percent = (int)(copied * 100L / total);
                        status(percent, "Копирование файла базы данных... " + percent.ToString(CultureInfo.InvariantCulture) + "%");
                    }
                }
            }

            CopyFileIfExists(source + "-wal", destination + "-wal");
            CopyFileIfExists(source + "-shm", destination + "-shm");

            if (status != null) status(100, "Снимок базы данных готов.");
        }

        private static void CopyFileIfExists(string source, string destination)
        {
            try
            {
                if (File.Exists(source))
                    File.Copy(source, destination, true);
            }
            catch (Exception ex)
            {
                Log.Warn("Не удалось скопировать " + source + ": " + ex.Message);
            }
        }

        private static void ValidateSchema(SqliteConnection connection)
        {
            string[] required = new string[] { "Apps", "Users", "IpsV4", "IpsV6", "Cnns", "Transfers" };
            List<string> missing = new List<string>();

            foreach (string name in required)
            {
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=?"))
                {
                    statement.Bind(1, name);
                    if (!statement.Step() || statement.GetInt64(0) == 0)
                        missing.Add(name);
                }
            }

            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "Файл не похож на базу статистики NetLimiter 5. Не найдены таблицы: " +
                    string.Join(", ", missing.ToArray()));
        }

        private void LoadDictionaries(SqliteConnection connection)
        {
            using (SqliteStatement statement = connection.Prepare("SELECT Id, Path FROM Apps"))
            {
                while (statement.Step())
                    _apps[statement.GetInt32(0)] = statement.GetString(1);
            }

            using (SqliteStatement statement = connection.Prepare("SELECT Id, Sid FROM Users"))
            {
                while (statement.Step())
                    _users[statement.GetInt32(0)] = statement.GetString(1);
            }

            using (SqliteStatement statement = connection.Prepare("SELECT Ip, DomName FROM IpsV4"))
            {
                while (statement.Step())
                    _v4[NormalizeV4(statement.GetInt64(0))] = statement.GetString(1);
            }

            using (SqliteStatement statement = connection.Prepare("SELECT Id, Ip1, Ip2, Ip3, Ip4, DomName FROM IpsV6"))
            {
                while (statement.Step())
                {
                    long id = statement.GetInt64(0);
                    string ip = IpTools.Ipv6(
                        statement.GetInt64(1),
                        statement.GetInt64(2),
                        statement.GetInt64(3),
                        statement.GetInt64(4));
                    _v6[id] = new Ipv6Info(ip, statement.GetString(5));
                }
            }
        }

        public DbStats LoadStats()
        {
            ThrowIfDisposed();
            DbStats stats = new DbStats();
            stats.Path = _sourcePath;
            stats.EffectivePath = _effectivePath;
            stats.IsSnapshot = _snapshot;

            try
            {
                stats.FileSize = new FileInfo(_effectivePath).Length;
            }
            catch
            {
                stats.FileSize = 0;
            }

            using (SqliteConnection connection = OpenConnection())
            {
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM Apps"))
                {
                    if (statement.Step()) stats.AppCount = statement.GetInt32(0);
                }
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM Users"))
                {
                    if (statement.Step()) stats.UserCount = statement.GetInt32(0);
                }
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM IpsV4"))
                {
                    if (statement.Step()) stats.V4Count = statement.GetInt32(0);
                }
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM IpsV6"))
                {
                    if (statement.Step()) stats.V6Count = statement.GetInt32(0);
                }
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM Cnns"))
                {
                    if (statement.Step()) stats.ConnectionCount = statement.GetInt64(0);
                }
                using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM Transfers"))
                {
                    if (statement.Step()) stats.TransferCount = statement.GetInt64(0);
                }
                using (SqliteStatement statement = connection.Prepare("SELECT MIN(TimeStart), MAX(TimeStart) FROM Cnns"))
                {
                    if (statement.Step() && !statement.IsNull(0))
                    {
                        stats.DataMinUtc = IpTools.FromWinMilliseconds(statement.GetInt64(0));
                        stats.DataMaxUtc = IpTools.FromWinMilliseconds(statement.GetInt64(1));
                    }
                }
                using (SqliteStatement statement = connection.Prepare("SELECT MAX(Time) FROM Transfers"))
                {
                    if (statement.Step() && !statement.IsNull(0))
                        stats.TransferMaxUtc = IpTools.FromUnixSeconds(statement.GetInt64(0));
                }
            }

            return stats;
        }

        public ConnectionsPage LoadConnectionsPage(ConnectionsPageRequest request)
        {
            ThrowIfDisposed();
            if (request == null) throw new ArgumentNullException("request");

            ConnectionsPage page = new ConnectionsPage();
            List<SqliteParameter> parameters = new List<SqliteParameter>();
            parameters.Add(new SqliteParameter(request.FromMs));
            parameters.Add(new SqliteParameter(request.ToMs));

            StringBuilder sql = new StringBuilder();
            sql.Append("SELECT rowid, TimeStart, TimeDuration, AppId, UserId, RemoteIpId, LocalIpId, RemotePort, LocalPort, Proto, Flags, TotalDataIn, TotalDataOut ");
            sql.Append("FROM Cnns WHERE TimeStart >= ? AND TimeStart <= ?");

            if (request.Proto > 0)
            {
                sql.Append(" AND Proto = ?");
                parameters.Add(new SqliteParameter(request.Proto));
            }

            if (request.AppId > 0)
            {
                sql.Append(" AND AppId = ?");
                parameters.Add(new SqliteParameter(request.AppId));
            }

            if (request.HasCursor)
            {
                sql.Append(" AND (TimeStart < ? OR (TimeStart = ? AND rowid < ?))");
                parameters.Add(new SqliteParameter(request.CursorTime));
                parameters.Add(new SqliteParameter(request.CursorTime));
                parameters.Add(new SqliteParameter(request.CursorRowId));
            }

            sql.Append(" ORDER BY TimeStart DESC, rowid DESC LIMIT ?");
            parameters.Add(new SqliteParameter(request.Limit));

            using (SqliteConnection connection = OpenConnection())
            using (SqliteStatement statement = connection.Prepare(sql.ToString()))
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (parameters[i].IsInteger)
                        statement.Bind(i + 1, parameters[i].Integer);
                    else
                        statement.Bind(i + 1, parameters[i].Text);
                }

                while (statement.Step())
                    page.Rows.Add(MapConnection(statement));
            }

            page.HasMore = page.Rows.Count >= request.Limit;
            if (page.Rows.Count > 0)
            {
                ConnectionRow last = page.Rows[page.Rows.Count - 1];
                page.LastTimeMs = last.TimeStartMs;
                page.LastRowId = last.RowId;
            }

            return page;
        }

        public long CountConnections(long fromMs, long toMs, int proto, int appId)
        {
            ThrowIfDisposed();
            StringBuilder sql = new StringBuilder();
            List<SqliteParameter> parameters = new List<SqliteParameter>();
            sql.Append("SELECT COUNT(*) FROM Cnns WHERE TimeStart >= ? AND TimeStart <= ?");
            parameters.Add(new SqliteParameter(fromMs));
            parameters.Add(new SqliteParameter(toMs));

            if (proto > 0)
            {
                sql.Append(" AND Proto = ?");
                parameters.Add(new SqliteParameter(proto));
            }

            if (appId > 0)
            {
                sql.Append(" AND AppId = ?");
                parameters.Add(new SqliteParameter(appId));
            }

            using (SqliteConnection connection = OpenConnection())
            using (SqliteStatement statement = connection.Prepare(sql.ToString()))
            {
                for (int i = 0; i < parameters.Count; i++)
                    statement.Bind(i + 1, parameters[i].Integer);

                if (statement.Step())
                    return statement.GetInt64(0);
                return 0;
            }
        }

        public List<AppAggregate> LoadAppSummary(long fromMs, long toMs, int proto, int appId, CancellationToken token)
        {
            ThrowIfDisposed();
            List<AppAggregate> result = new List<AppAggregate>();

            StringBuilder sql = new StringBuilder();
            List<SqliteParameter> parameters = new List<SqliteParameter>();
            sql.Append("SELECT AppId, COUNT(*), COALESCE(SUM(TotalDataIn), 0), COALESCE(SUM(TotalDataOut), 0), MAX(TimeStart), COUNT(DISTINCT RemoteIpId) ");
            sql.Append("FROM Cnns WHERE TimeStart >= ? AND TimeStart <= ?");
            parameters.Add(new SqliteParameter(fromMs));
            parameters.Add(new SqliteParameter(toMs));

            if (proto > 0)
            {
                sql.Append(" AND Proto = ?");
                parameters.Add(new SqliteParameter(proto));
            }

            if (appId > 0)
            {
                sql.Append(" AND AppId = ?");
                parameters.Add(new SqliteParameter(appId));
            }

            sql.Append(" GROUP BY AppId ORDER BY SUM(TotalDataIn + TotalDataOut) DESC");

            using (SqliteConnection connection = OpenConnection())
            using (SqliteStatement statement = connection.Prepare(sql.ToString()))
            {
                for (int i = 0; i < parameters.Count; i++)
                    statement.Bind(i + 1, parameters[i].Integer);

                while (statement.Step())
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException(token);

                    AppAggregate row = new AppAggregate();
                    row.AppId = statement.GetInt32(0);
                    row.AppPath = GetAppPath(row.AppId);
                    row.Connections = statement.GetInt64(1);
                    row.InBytes = statement.GetInt64(2);
                    row.OutBytes = statement.GetInt64(3);
                    row.LastUtc = IpTools.FromWinMilliseconds(statement.GetInt64(4));
                    row.UniqueRemoteIps = statement.GetInt64(5);
                    row.Category = _classifier.Classify(row.AppPath, "", "", 0, 0, "").Name;
                    result.Add(row);
                }
            }

            return result;
        }

        public List<DomainRow> LoadDomains(long fromMs, long toMs, int proto, int appId, CancellationToken token)
        {
            ThrowIfDisposed();
            Dictionary<string, DomainAccumulator> map = new Dictionary<string, DomainAccumulator>(StringComparer.Ordinal);

            StringBuilder sql = new StringBuilder();
            List<SqliteParameter> parameters = new List<SqliteParameter>();
            sql.Append("SELECT RemoteIpId, (Flags & 2), Proto, COUNT(*), COALESCE(SUM(TotalDataIn), 0), COALESCE(SUM(TotalDataOut), 0), MAX(TimeStart) ");
            sql.Append("FROM Cnns WHERE TimeStart >= ? AND TimeStart <= ?");
            parameters.Add(new SqliteParameter(fromMs));
            parameters.Add(new SqliteParameter(toMs));

            if (proto > 0)
            {
                sql.Append(" AND Proto = ?");
                parameters.Add(new SqliteParameter(proto));
            }

            if (appId > 0)
            {
                sql.Append(" AND AppId = ?");
                parameters.Add(new SqliteParameter(appId));
            }

            sql.Append(" GROUP BY RemoteIpId, (Flags & 2), Proto");

            using (SqliteConnection connection = OpenConnection())
            using (SqliteStatement statement = connection.Prepare(sql.ToString()))
            {
                for (int i = 0; i < parameters.Count; i++)
                    statement.Bind(i + 1, parameters[i].Integer);

                while (statement.Step())
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException(token);

                    long remoteId = statement.GetInt64(0);
                    bool ipv6 = (statement.GetInt64(1) & 2) != 0;
                    int protocol = statement.GetInt32(2);
                    long connections = statement.GetInt64(3);
                    long inBytes = statement.GetInt64(4);
                    long outBytes = statement.GetInt64(5);
                    DateTime last = IpTools.FromWinMilliseconds(statement.GetInt64(6));

                    string normalizedKey;
                    if (ipv6)
                        normalizedKey = "6:" + remoteId.ToString(CultureInfo.InvariantCulture);
                    else
                        normalizedKey = "4:" + NormalizeV4(remoteId).ToString(CultureInfo.InvariantCulture);

                    DomainAccumulator acc;
                    if (!map.TryGetValue(normalizedKey, out acc))
                    {
                        acc = new DomainAccumulator();
                        acc.Row = new DomainRow();
                        acc.Row.IsIpv6 = ipv6;
                        string domainName;
                        acc.Row.Ip = ResolveRemoteIp(remoteId, ipv6, out domainName);
                        acc.Row.Domain = domainName;
                        acc.Protocols = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                        map[normalizedKey] = acc;
                    }

                    string protocolName = Protocols.Name(protocol);
                    acc.Row.Connections += connections;
                    acc.Row.InBytes += inBytes;
                    acc.Row.OutBytes += outBytes;
                    if (last > acc.Row.LastUtc) acc.Row.LastUtc = last;
                    acc.Protocols.Add(protocolName);

                    string category = _classifier.ClassifyService(acc.Row.Domain, acc.Row.Ip, 0, protocolName).Name;
                    long traffic = inBytes + outBytes;
                    if (traffic > acc.BestCategoryBytes || string.IsNullOrEmpty(acc.Row.Service))
                    {
                        acc.BestCategoryBytes = traffic;
                        acc.Row.Service = category;
                    }
                }
            }

            List<DomainRow> result = new List<DomainRow>(map.Count);
            foreach (DomainAccumulator acc in map.Values)
            {
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                acc.Row.TotalBytes = acc.Row.InBytes + acc.Row.OutBytes;
                acc.Row.Protocols = string.Join(", ", new List<string>(acc.Protocols).ToArray());
                if (string.IsNullOrEmpty(acc.Row.Service))
                    acc.Row.Service = _classifier.Fallback.Name;
                result.Add(acc.Row);
            }

            result.Sort(delegate(DomainRow a, DomainRow b)
            {
                int cmp = b.TotalBytes.CompareTo(a.TotalBytes);
                if (cmp != 0) return cmp;
                return string.Compare(a.Ip, b.Ip, StringComparison.OrdinalIgnoreCase);
            });

            return result;
        }

        public TransferSummary LoadTransfers(
            DateTime fromUtc,
            DateTime toUtc,
            int protoFilter,
            int appFilter,
            CancellationToken token,
            IProgress<TransferProgress> progress)
        {
            ThrowIfDisposed();

            long fromUnix = IpTools.ToUnixSeconds(fromUtc);
            long toUnix = IpTools.ToUnixSeconds(toUtc);

            int totalBlobs = 0;
            using (SqliteConnection connection = OpenConnection())
            using (SqliteStatement statement = connection.Prepare("SELECT COUNT(*) FROM Transfers WHERE Time >= ? AND Time <= ?"))
            {
                statement.Bind(1, fromUnix);
                statement.Bind(2, toUnix);
                if (statement.Step()) totalBlobs = (int)statement.GetInt64(0);
            }

            TransferProgress current = new TransferProgress();
            current.BlobsTotal = totalBlobs;
            if (progress != null) progress.Report(current);

            Dictionary<TransferGroupKey, TransferBucket> buckets = new Dictionary<TransferGroupKey, TransferBucket>();
            TransferSummary summary = new TransferSummary();
            summary.Blobs = totalBlobs;
            bool hasFirst = false;

            using (SqliteConnection connection = OpenConnection())
            using (SqliteStatement statement = connection.Prepare("SELECT Time, Data FROM Transfers WHERE Time >= ? AND Time <= ? ORDER BY Time DESC"))
            {
                statement.Bind(1, fromUnix);
                statement.Bind(2, toUnix);

                int blobIndex = 0;
                while (statement.Step())
                {
                    if (token.IsCancellationRequested) throw new OperationCanceledException(token);

                    long unixTime = statement.GetInt64(0);
                    byte[] data = statement.GetBlob(1);
                    DateTime blobTime = IpTools.FromUnixSeconds(unixTime);
                    blobIndex++;

                    if (!hasFirst)
                    {
                        summary.FirstUtc = blobTime;
                        hasFirst = true;
                    }
                    if (blobTime > summary.LastUtc) summary.LastUtc = blobTime;

                    int records = data.Length / 30;
                    for (int index = 0; index < records; index++)
                    {
                        int offset = index * 30;

                        // В записи Transfers первые 6 байт — исходящий трафик,
                        // затем порт, затем 6 байт входящего (сверено с Cnns).
                        long outBytes = ReadUInt48(data, offset);
                        int remotePort = data[offset + 6] | (data[offset + 7] << 8);
                        long inBytes = ReadUInt48(data, offset + 8);
                        int localPort = data[offset + 14] | (data[offset + 15] << 8);
                        long remoteId = ReadUInt32(data, offset + 16);
                        long localId = ReadUInt32(data, offset + 20);
                        int appId = data[offset + 24] | (data[offset + 25] << 8);
                        int protocol = data[offset + 28];
                        bool ipv6 = (data[offset + 29] & 2) != 0;

                        if (appFilter > 0 && appId != appFilter) continue;
                        if (protoFilter > 0 && protocol != protoFilter) continue;

                        TransferGroupKey key = new TransferGroupKey();
                        key.AppId = appId;
                        key.RemotePort = remotePort;
                        key.RemoteId = ipv6 ? remoteId : NormalizeV4(remoteId);
                        key.LocalId = ipv6 ? localId : NormalizeV4(localId);
                        key.Proto = (byte)protocol;
                        key.Ipv6 = ipv6;

                        TransferBucket bucket;
                        if (!buckets.TryGetValue(key, out bucket))
                        {
                            bucket = new TransferBucket();
                            bucket.AppId = appId;
                            bucket.RemotePort = remotePort;
                            bucket.LocalPort = localPort;
                            bucket.Proto = protocol;
                            bucket.Ipv6 = ipv6;
                            bucket.AppPath = GetAppPath(appId);
                            string bucketDomain;
                            bucket.RemoteIp = ResolveRemoteIp(remoteId, ipv6, out bucketDomain);
                            bucket.Domain = bucketDomain;
                            bucket.LocalIp = ResolveLocalIp(localId, ipv6);
                            bucket.Category = _classifier.Classify(
                                bucket.AppPath,
                                bucket.Domain,
                                bucket.RemoteIp,
                                remotePort,
                                0,
                                Protocols.Name(protocol)).Name;
                            buckets[key] = bucket;
                        }

                        bucket.InBytes += inBytes;
                        bucket.OutBytes += outBytes;
                        bucket.Records++;
                        if (!bucket.HasTime)
                        {
                            bucket.FirstUtc = blobTime;
                            bucket.LastUtc = blobTime;
                            bucket.HasTime = true;
                        }
                        else
                        {
                            if (blobTime < bucket.FirstUtc) bucket.FirstUtc = blobTime;
                            if (blobTime > bucket.LastUtc) bucket.LastUtc = blobTime;
                        }

                        summary.TotalIn += inBytes;
                        summary.TotalOut += outBytes;
                        summary.TotalRecords++;
                    }

                    if (progress != null && (blobIndex % 100 == 0 || blobIndex == totalBlobs))
                    {
                        current.BlobsDone = blobIndex;
                        current.Records = summary.TotalRecords;
                        progress.Report(current);
                    }

                    if ((blobIndex & 0x3F) == 0 && token.IsCancellationRequested)
                        throw new OperationCanceledException(token);
                }
            }

            List<TrafficRow> rows = new List<TrafficRow>(buckets.Count);
            foreach (TransferBucket bucket in buckets.Values)
            {
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);

                TrafficRow row = new TrafficRow();
                row.AppId = bucket.AppId;
                row.AppPath = bucket.AppPath;
                row.Category = bucket.Category;
                row.Proto = bucket.Proto;
                row.LocalIp = bucket.LocalIp;
                row.RemoteIp = bucket.RemoteIp;
                row.RemotePort = bucket.RemotePort;
                row.Domain = bucket.Domain;
                row.InBytes = bucket.InBytes;
                row.OutBytes = bucket.OutBytes;
                row.TotalBytes = bucket.InBytes + bucket.OutBytes;
                row.RecordCount = bucket.Records;
                row.FirstUtc = bucket.FirstUtc;
                row.LastUtc = bucket.LastUtc;
                row.IsIpv6 = bucket.Ipv6;
                rows.Add(row);
            }

            rows.Sort(delegate(TrafficRow a, TrafficRow b)
            {
                int cmp = b.TotalBytes.CompareTo(a.TotalBytes);
                if (cmp != 0) return cmp;
                cmp = b.RecordCount.CompareTo(a.RecordCount);
                if (cmp != 0) return cmp;
                return string.Compare(a.RemoteIp, b.RemoteIp, StringComparison.OrdinalIgnoreCase);
            });

            summary.Rows = rows;
            summary.Groups = rows.Count;

            if (progress != null)
            {
                current.BlobsDone = totalBlobs;
                current.Records = summary.TotalRecords;
                progress.Report(current);
            }

            return summary;
        }

        private ConnectionRow MapConnection(SqliteStatement statement)
        {
            ConnectionRow row = new ConnectionRow();
            row.RowId = statement.GetInt64(0);
            row.TimeStartMs = statement.GetInt64(1);
            row.TimeUtc = IpTools.FromWinMilliseconds(row.TimeStartMs);
            row.DurationSec = statement.GetInt64(2) / 1000.0;
            row.AppId = statement.GetInt32(3);
            row.UserId = statement.GetInt32(4);
            long remoteId = statement.GetInt64(5);
            long localId = statement.GetInt64(6);
            row.RemotePort = statement.GetInt32(7);
            row.LocalPort = statement.GetInt32(8);
            row.Proto = statement.GetInt32(9);
            row.Flags = statement.GetInt32(10);
            row.InBytes = statement.GetInt64(11);
            row.OutBytes = statement.GetInt64(12);

            bool ipv6 = (row.Flags & 2) != 0;
            string domainName;
            row.RemoteIp = ResolveRemoteIp(remoteId, ipv6, out domainName);
            row.Domain = domainName;
            row.LocalIp = ResolveLocalIp(localId, ipv6);
            row.AppPath = GetAppPath(row.AppId);
            row.Category = _classifier.Classify(
                row.AppPath,
                row.Domain,
                row.RemoteIp,
                row.RemotePort,
                row.LocalPort,
                Protocols.Name(row.Proto)).Name;

            return row;
        }

        private string GetAppPath(int appId)
        {
            string path;
            return _apps.TryGetValue(appId, out path) ? path : "Приложение #" + appId.ToString(CultureInfo.InvariantCulture);
        }

        private string ResolveRemoteIp(long id, bool ipv6, out string domain)
        {
            if (ipv6)
            {
                Ipv6Info info;
                if (_v6.TryGetValue(id, out info))
                {
                    domain = info.Domain;
                    return info.Ip;
                }
                domain = "";
                return "IPv6 #" + id.ToString(CultureInfo.InvariantCulture);
            }

            long key = NormalizeV4(id);
            string name;
            if (!_v4.TryGetValue(key, out name)) name = "";
            domain = name ?? "";
            return IpTools.Ipv4(id);
        }

        private string ResolveLocalIp(long id, bool ipv6)
        {
            if (ipv6)
            {
                Ipv6Info info;
                if (_v6.TryGetValue(id, out info)) return info.Ip;
                return "IPv6 #" + id.ToString(CultureInfo.InvariantCulture);
            }
            return IpTools.Ipv4(id);
        }

        private static long NormalizeV4(long value)
        {
            return (long)unchecked((uint)value);
        }

        private static long ReadUInt48(byte[] data, int offset)
        {
            return (long)data[offset]
                | ((long)data[offset + 1] << 8)
                | ((long)data[offset + 2] << 16)
                | ((long)data[offset + 3] << 24)
                | ((long)data[offset + 4] << 32)
                | ((long)data[offset + 5] << 40);
        }

        private static long ReadUInt32(byte[] data, int offset)
        {
            return (uint)(data[offset]
                | (data[offset + 1] << 8)
                | (data[offset + 2] << 16)
                | (data[offset + 3] << 24));
        }

        private SqliteConnection OpenConnection()
        {
            ThrowIfDisposed();
            return new SqliteConnection(_effectivePath);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException("NetLimiterDatabase");
        }

        public void Dispose()
        {
            _disposed = true;
        }

        private sealed class SqliteParameter
        {
            public bool IsInteger;
            public long Integer;
            public string Text;

            public SqliteParameter(long value)
            {
                IsInteger = true;
                Integer = value;
            }

            public SqliteParameter(string value)
            {
                IsInteger = false;
                Text = value;
            }
        }
    }
}
