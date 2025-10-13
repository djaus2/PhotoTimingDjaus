using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace AthStitcher.Data
{
    public static class DbInitializer
    {
        public static void Initialize()
        {
            // Ensure database exists, then ensure tables exist
            EnsureDatabase();
            EnsureTables();
        }

        public static void EnsureDatabase()
        {
            // For SQLite, opening the connection ensures the file exists.
            using var conn = Db.CreateConnection();
            // no-op: file is created when connection opens; foreign keys pragma is set in Db.CreateConnection
        }

        private static void EnsureTables()
        {
            using var conn = Db.CreateConnection();
            using var cmd = conn.CreateCommand();

            // Users (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Users (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Username TEXT NOT NULL UNIQUE,
  PasswordHash BLOB NOT NULL,
  PasswordSalt BLOB NOT NULL,
  Role TEXT NOT NULL,
  CreatedAt TEXT NOT NULL,
  ForcePasswordChange INTEGER NOT NULL DEFAULT 0
);";
            cmd.ExecuteNonQuery();

            // Meets (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Meets (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Name TEXT NOT NULL,
  Date TEXT NULL,
  Location TEXT NULL
);";
            cmd.ExecuteNonQuery();

            // Events (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Events (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  MeetId INTEGER NOT NULL,
  EventNumber INTEGER NULL,
  HeatNumber INTEGER NULL,
  Name TEXT NULL,
  Distance INTEGER NULL,
  HurdleSteepleHeight INTEGER NULL,
  Sex TEXT NULL,
  FOREIGN KEY (MeetId) REFERENCES Meets(Id) ON DELETE CASCADE
);";
            cmd.ExecuteNonQuery();

            // Results (SQLite)
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Results (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EventId INTEGER NOT NULL,
  Lane INTEGER NULL,
  BibNumber INTEGER NULL,
  Name TEXT NULL,
  ResultSeconds REAL NULL,
  FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
);";
            cmd.ExecuteNonQuery();
        }
    }
}
