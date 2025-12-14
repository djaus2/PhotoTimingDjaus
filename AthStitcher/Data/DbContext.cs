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
            var saveDir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir!);
            }
            optionsBuilder
                .UseSqlite($"Data Source={dbPath}")
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .UseLazyLoadingProxies(); // This now works because of the added using directive
                //.UseChangeTrackingProxies();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly configure Event -> Heats relationship with cascade delete.
            // Heat.EventId is non-nullable so cascade is appropriate: deleting an Event removes its Heats.
            modelBuilder.Entity<Heat>()
                .HasOne(h => h.Event)
                .WithMany(e => e.Heats)
                .HasForeignKey(h => h.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optional: make Meet -> Events explicit too (choose desired delete behavior).
            // Example: cascade delete Events when a Meet is removed:
            modelBuilder.Entity<Event>()
                .HasOne(ev => ev.Meet)
                .WithMany(m => m.Events)
                .HasForeignKey(ev => ev.MeetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Or to prevent accidental removal of Events when a Meet is deleted, use Restrict:
            // .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
