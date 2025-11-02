using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Proxies;
using System;
using System.IO;

namespace AthStitcher.Data
{
    public class AthStitcherDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Meet> Meets { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Heat> Heats { get; set; }
        public DbSet<LaneResult> Results { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AthStitcher", "athstitcher.db");
            var dir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir!);
            }
            optionsBuilder
                .UseSqlite($"Data Source={dbPath}")
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .UseLazyLoadingProxies(); // This now works because of the added using directive
                //.UseChangeTrackingProxies();
        }

    }
}
