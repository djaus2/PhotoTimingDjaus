using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using System.IO;

namespace AthStitcher.Data
{
    public static class Db
    {
        // SQLite DB path: %LocalAppData%/AthStitcher/athstitcher.db
        public static string GetConnectionString()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AthStitcher");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "athstitcher.db");
            var cs = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
            return cs;
        }

        public static DbConnection CreateConnection(bool open = true)
        {
            var conn = new SqliteConnection(GetConnectionString());
            if (open)
            {
                conn.Open();
                // Ensure foreign keys are enforced
                using var pragma = conn.CreateCommand();
                pragma.CommandText = "PRAGMA foreign_keys = ON;";
                pragma.ExecuteNonQuery();
            }
            return conn;
        }
    }
}
