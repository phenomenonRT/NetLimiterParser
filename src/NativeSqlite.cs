using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NetLimiterParser
{
    internal sealed class SqliteException : Exception
    {
        public int Code { get; private set; }

        public SqliteException(int code, string message)
            : base(message)
        {
            Code = code;
        }
    }

    internal static class NativeSqlite
    {
        public const int SQLITE_OK = 0;
        public const int SQLITE_ROW = 100;
        public const int SQLITE_DONE = 101;
        public const int SQLITE_OPEN_READONLY = 0x00000001;
        public const int SQLITE_OPEN_READWRITE = 0x00000002;
        public const int SQLITE_OPEN_CREATE = 0x00000004;
        public const int SQLITE_BUSY = 5;
        public const int SQLITE_LOCKED = 6;
        public const int SQLITE_NULL = 5;

        public static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_open_v2(
            [MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
            out IntPtr db,
            int flags,
            IntPtr vfs);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_close_v2(IntPtr db);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_busy_timeout(IntPtr db, int milliseconds);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_prepare_v2(
            IntPtr db,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string sql,
            int nByte,
            out IntPtr stmt,
            IntPtr tail);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_step(IntPtr stmt);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_finalize(IntPtr stmt);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_bind_text(
            IntPtr stmt,
            int index,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string value,
            int n,
            IntPtr destructor);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_count(IntPtr stmt);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_type(IntPtr stmt, int index);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern long sqlite3_column_int64(IntPtr stmt, int index);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_column_text(IntPtr stmt, int index);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_column_blob(IntPtr stmt, int index);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_column_bytes(IntPtr stmt, int index);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_errmsg(IntPtr db);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_libversion();

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr sqlite3_backup_init(
            IntPtr destDb,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string destName,
            IntPtr sourceDb,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string sourceName);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_step(IntPtr backup, int pages);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_finish(IntPtr backup);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_remaining(IntPtr backup);

        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int sqlite3_backup_pagecount(IntPtr backup);

        public static string Version()
        {
            try
            {
                IntPtr p = sqlite3_libversion();
                return p == IntPtr.Zero ? "?" : Marshal.PtrToStringAnsi(p);
            }
            catch
            {
                return "?";
            }
        }

        public static string ErrorMessage(IntPtr db)
        {
            try
            {
                if (db == IntPtr.Zero) return "SQLite: нет соединения";
                IntPtr p = sqlite3_errmsg(db);
                return p == IntPtr.Zero ? "SQLite: неизвестная ошибка" : Marshal.PtrToStringAnsi(p);
            }
            catch
            {
                return "SQLite: неизвестная ошибка";
            }
        }
    }

    internal sealed class SqliteConnection : IDisposable
    {
        private IntPtr _db;
        private bool _disposed;
        private readonly string _path;

        public string Path
        {
            get { return _path; }
        }

        public IntPtr Handle
        {
            get { return _db; }
        }

        public bool IsOpen
        {
            get { return !_disposed && _db != IntPtr.Zero; }
        }

        public SqliteConnection(string path)
        {
            _path = path;
            IntPtr handle;
            int rc = NativeSqlite.sqlite3_open_v2(path, out handle, NativeSqlite.SQLITE_OPEN_READONLY, IntPtr.Zero);
            _db = handle;

            if (rc != NativeSqlite.SQLITE_OK)
            {
                string message = NativeSqlite.ErrorMessage(handle);
                if (handle != IntPtr.Zero)
                {
                    NativeSqlite.sqlite3_close_v2(handle);
                    _db = IntPtr.Zero;
                }
                throw new SqliteException(rc, "Не удалось открыть базу данных. " + message);
            }

            NativeSqlite.sqlite3_busy_timeout(_db, 8000);
        }

        public void Execute(string sql)
        {
            using (SqliteStatement st = Prepare(sql))
            {
                st.Step();
            }
        }

        public SqliteStatement Prepare(string sql)
        {
            if (_disposed) throw new ObjectDisposedException("SqliteConnection");
            if (_db == IntPtr.Zero) throw new InvalidOperationException("Соединение SQLite закрыто.");

            IntPtr stmt;
            int rc = NativeSqlite.sqlite3_prepare_v2(_db, sql, -1, out stmt, IntPtr.Zero);
            if (rc != NativeSqlite.SQLITE_OK)
            {
                throw new SqliteException(rc, "Ошибка SQLite при подготовке запроса: " + NativeSqlite.ErrorMessage(_db) + Environment.NewLine + sql);
            }
            if (stmt == IntPtr.Zero)
                throw new SqliteException(rc, "SQLite не вернул выражение для запроса: " + sql);

            return new SqliteStatement(this, stmt);
        }

        public string ErrorMessage()
        {
            return NativeSqlite.ErrorMessage(_db);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_db != IntPtr.Zero)
            {
                NativeSqlite.sqlite3_close_v2(_db);
                _db = IntPtr.Zero;
            }
        }
    }

    internal sealed class SqliteStatement : IDisposable
    {
        private readonly SqliteConnection _connection;
        private IntPtr _stmt;
        private bool _disposed;

        public SqliteStatement(SqliteConnection connection, IntPtr statement)
        {
            _connection = connection;
            _stmt = statement;
        }

        public void Bind(int index, long value)
        {
            int rc = NativeSqlite.sqlite3_bind_int64(_stmt, index, value);
            if (rc != NativeSqlite.SQLITE_OK)
                throw new SqliteException(rc, "Ошибка привязки числа: " + _connection.ErrorMessage());
        }

        public void Bind(int index, string value)
        {
            int rc = NativeSqlite.sqlite3_bind_text(_stmt, index, value ?? "", -1, NativeSqlite.SQLITE_TRANSIENT);
            if (rc != NativeSqlite.SQLITE_OK)
                throw new SqliteException(rc, "Ошибка привязки строки: " + _connection.ErrorMessage());
        }

        public bool Step()
        {
            int rc = NativeSqlite.sqlite3_step(_stmt);
            if (rc == NativeSqlite.SQLITE_ROW) return true;
            if (rc == NativeSqlite.SQLITE_DONE) return false;
            throw new SqliteException(rc, "Ошибка выполнения запроса: " + _connection.ErrorMessage());
        }

        public bool IsNull(int index)
        {
            return NativeSqlite.sqlite3_column_type(_stmt, index) == NativeSqlite.SQLITE_NULL;
        }

        public long GetInt64(int index)
        {
            return NativeSqlite.sqlite3_column_int64(_stmt, index);
        }

        public int GetInt32(int index)
        {
            return (int)NativeSqlite.sqlite3_column_int64(_stmt, index);
        }

        public string GetString(int index)
        {
            IntPtr ptr = NativeSqlite.sqlite3_column_text(_stmt, index);
            int length = NativeSqlite.sqlite3_column_bytes(_stmt, index);
            if (ptr == IntPtr.Zero || length <= 0) return "";
            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        public byte[] GetBlob(int index)
        {
            IntPtr ptr = NativeSqlite.sqlite3_column_blob(_stmt, index);
            int length = NativeSqlite.sqlite3_column_bytes(_stmt, index);
            if (ptr == IntPtr.Zero || length <= 0) return new byte[0];
            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return buffer;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_stmt != IntPtr.Zero)
            {
                NativeSqlite.sqlite3_finalize(_stmt);
                _stmt = IntPtr.Zero;
            }
        }
    }
}
