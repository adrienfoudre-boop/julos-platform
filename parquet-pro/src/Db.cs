using System.Data;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ParquetPro;

public sealed class DbCtx : IDisposable
{
    public readonly SqliteConnection Cn;
    SqliteTransaction tx;
    readonly bool locked;

    public DbCtx(bool transaction = false)
    {
        if (transaction) { Monitor.Enter(Db.WriteLock); locked = true; }
        try
        {
            Cn = Db.Open();
            if (transaction) tx = Cn.BeginTransaction();
        }
        catch
        {
            if (locked) Monitor.Exit(Db.WriteLock);
            throw;
        }
    }

    SqliteCommand Cmd(string sql, object[] a)
    {
        var c = Cn.CreateCommand();
        c.CommandText = sql;
        c.Transaction = tx;
        c.CommandTimeout = 60;
        for (int i = 0; i < a.Length; i++) c.Parameters.AddWithValue("@p" + i, Db.Norm(a[i]));
        return c;
    }

    public int Exec(string sql, params object[] a) { using var c = Cmd(sql, a); return c.ExecuteNonQuery(); }
    public object Scalar(string sql, params object[] a) { using var c = Cmd(sql, a); var r = c.ExecuteScalar(); return r == DBNull.Value ? null : r; }
    public long Long(string sql, params object[] a) { var r = Scalar(sql, a); return r == null ? 0 : Convert.ToInt64(r, CultureInfo.InvariantCulture); }
    public double Dbl(string sql, params object[] a) { var r = Scalar(sql, a); return r == null ? 0 : Convert.ToDouble(r, CultureInfo.InvariantCulture); }
    public string Str(string sql, params object[] a) => Scalar(sql, a)?.ToString();
    public long Insert(string sql, params object[] a)
    {
        using var c = Cmd(sql + "; SELECT last_insert_rowid();", a);
        return Convert.ToInt64(c.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public List<object[]> Rows(string sql, params object[] a)
    {
        var list = new List<object[]>();
        using var c = Cmd(sql, a);
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            var row = new object[r.FieldCount];
            for (int i = 0; i < r.FieldCount; i++) row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
            list.Add(row);
        }
        return list;
    }

    public DataTable Table(string sql, params object[] a)
    {
        var dt = new DataTable();
        using var c = Cmd(sql, a);
        using var r = c.ExecuteReader();
        int n = r.FieldCount;
        var names = new string[n];
        for (int i = 0; i < n; i++) names[i] = r.GetName(i);
        var rows = new List<object[]>();
        while (r.Read())
        {
            var row = new object[n];
            for (int i = 0; i < n; i++) row[i] = r.IsDBNull(i) ? null : r.GetValue(i);
            rows.Add(row);
        }
        var types = new Type[n];
        for (int i = 0; i < n; i++)
        {
            Type t = null; bool mixed = false;
            foreach (var row in rows)
            {
                var v = row[i]; if (v == null) continue;
                var vt = v is long || v is int ? typeof(long) : v is double || v is float ? typeof(double) : typeof(string);
                if (t == null) t = vt;
                else if (t != vt) { if ((t == typeof(long) && vt == typeof(double)) || (t == typeof(double) && vt == typeof(long))) t = typeof(double); else mixed = true; }
            }
            types[i] = mixed || t == null ? typeof(string) : t;
            string nm = names[i]; int k = 2;
            while (dt.Columns.Contains(nm)) nm = names[i] + (k++);
            dt.Columns.Add(nm, types[i]);
        }
        dt.BeginLoadData();
        foreach (var row in rows)
        {
            var vals = new object[n];
            for (int i = 0; i < n; i++)
            {
                var v = row[i];
                if (v == null) vals[i] = DBNull.Value;
                else if (types[i] == typeof(string)) vals[i] = Convert.ToString(v, CultureInfo.InvariantCulture);
                else if (types[i] == typeof(double)) vals[i] = Convert.ToDouble(v, CultureInfo.InvariantCulture);
                else vals[i] = Convert.ToInt64(v, CultureInfo.InvariantCulture);
            }
            dt.Rows.Add(vals);
        }
        dt.EndLoadData();
        return dt;
    }

    public string Get(string key, string def = null) => Str("SELECT valeur FROM parametres WHERE cle=@p0", key) ?? def;
    public void Set(string key, object val) => Exec("INSERT INTO parametres(cle,valeur) VALUES(@p0,@p1) ON CONFLICT(cle) DO UPDATE SET valeur=excluded.valeur", key, Convert.ToString(val, CultureInfo.InvariantCulture));

    public void Commit() { tx?.Commit(); tx?.Dispose(); tx = null; }

    public void Dispose()
    {
        try { if (tx != null) { try { tx.Rollback(); } catch { } tx.Dispose(); tx = null; } Cn?.Dispose(); }
        finally { if (locked) Monitor.Exit(Db.WriteLock); }
    }
}

public static class Db
{
    public static string Dir, PathDb, BackupDir, ExportDir, LogFile;
    public static readonly object WriteLock = new();
    static string cs;

    public static void Init()
    {
        Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ParquetPro");
        Directory.CreateDirectory(Dir);
        BackupDir = Path.Combine(Dir, "Sauvegardes"); Directory.CreateDirectory(BackupDir);
        ExportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PARQUET PRO - Exports");
        try { Directory.CreateDirectory(ExportDir); } catch { ExportDir = Path.Combine(Dir, "Exports"); Directory.CreateDirectory(ExportDir); }
        LogFile = Path.Combine(Dir, "parquetpro.log");
        PathDb = Path.Combine(Dir, "parquetpro.db");
        cs = new SqliteConnectionStringBuilder { DataSource = PathDb, Pooling = true, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
    }

    public static SqliteConnection Open()
    {
        var c = new SqliteConnection(cs);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA busy_timeout=15000; PRAGMA foreign_keys=OFF; PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();
        return c;
    }

    public static object Norm(object v)
    {
        if (v == null) return DBNull.Value;
        if (v is DateTime d) return U.D(d);
        if (v is bool b) return b ? 1 : 0;
        if (v is Enum) return v.ToString();
        return v;
    }

    public static T Q<T>(Func<DbCtx, T> f) { using var c = new DbCtx(); return f(c); }
    public static void Tx(Action<DbCtx> f) { using var c = new DbCtx(true); f(c); c.Commit(); }
    public static T Tx<T>(Func<DbCtx, T> f) { using var c = new DbCtx(true); var r = f(c); c.Commit(); return r; }

    public static int Exec(string sql, params object[] a) => Tx(c => c.Exec(sql, a));
    public static long Insert(string sql, params object[] a) => Tx(c => c.Insert(sql, a));
    public static object Scalar(string sql, params object[] a) => Q(c => c.Scalar(sql, a));
    public static long Long(string sql, params object[] a) => Q(c => c.Long(sql, a));
    public static double Dbl(string sql, params object[] a) => Q(c => c.Dbl(sql, a));
    public static string Str(string sql, params object[] a) => Q(c => c.Str(sql, a));
    public static DataTable Table(string sql, params object[] a) => Q(c => c.Table(sql, a));
    public static List<object[]> Rows(string sql, params object[] a) => Q(c => c.Rows(sql, a));
    public static string Get(string key, string def = null) => Q(c => c.Get(key, def));
    public static void Set(string key, object val) => Tx(c => c.Set(key, val));

    public static string Backup(string label = "auto")
    {
        string file = Path.Combine(BackupDir, $"parquetpro_{DateTime.Now:yyyyMMdd_HHmmss}_{label}.db");
        lock (WriteLock)
        {
            using var src = Open();
            using var dst = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false }.ToString());
            dst.Open();
            src.BackupDatabase(dst);
        }
        try
        {
            var files = new DirectoryInfo(BackupDir).GetFiles("parquetpro_*_auto.db").OrderByDescending(f => f.CreationTime).Skip(12).ToList();
            foreach (var f in files) f.Delete();
        }
        catch { }
        return file;
    }

    public static void RestoreFrom(string file)
    {
        lock (WriteLock)
        {
            SqliteConnection.ClearAllPools();
            using (var src = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString()))
            {
                src.Open();
                using var chk = src.CreateCommand();
                chk.CommandText = "SELECT COUNT(*) FROM collaborateurs";
                chk.ExecuteScalar();
                using var dst = Open();
                src.BackupDatabase(dst);
            }
            SqliteConnection.ClearAllPools();
        }
    }
}
