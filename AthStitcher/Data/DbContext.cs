using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace AthStitcher.Data
{
    public class AthStitcherDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Meet> Meets { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AthStitcher", "athstitcher.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Username).IsRequired();
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.PasswordSalt).IsRequired();
                e.Property(x => x.Role).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
                e.Property(x => x.ForcePasswordChange).HasDefaultValue(false).IsRequired();
            });

            // Meets
            modelBuilder.Entity<Meet>(e =>
            {
                e.ToTable("Meets");
                e.HasKey(x => x.Id);
                e.Property(x => x.Description).IsRequired();
                e.Property(x => x.Date).HasColumnType("TEXT").IsRequired(false);
                e.Property(x => x.Location).HasColumnType("TEXT").IsRequired(false);
            });

            // Events
            modelBuilder.Entity<Event>(e =>
            {
                e.ToTable("Events");
                e.HasKey(x => x.Id);
                e.Property(x => x.EventNumber).IsRequired(false);
                e.Property(x => x.HeatNumber).IsRequired(false);
                e.Property(x => x.Time).HasColumnType("TEXT").IsRequired(false); // stored as TEXT (ISO-8601)
                e.Property(x => x.Description).IsRequired(false);
                e.Property(x => x.Distance).IsRequired(false);
                e.Property(x => x.HurdleSteepleHeight).IsRequired(false);
                e.Property(x => x.Sex).IsRequired(false);
                // enums as ints
                e.Property(x => x.TrackType).HasConversion<int>().HasDefaultValue(TrackType.na).IsRequired();
                e.Property(x => x.Gender).HasConversion<int>().HasDefaultValue(Gender.none).IsRequired();
                e.Property(x => x.AgeGrouping).HasConversion<int>().HasDefaultValue(AgeGrouping.none).IsRequired();
                e.Property(x => x.StandardAgeGroup).HasConversion<int>().IsRequired(false);
                e.Property(x => x.MastersAgeGroup).HasConversion<int>().IsRequired(false);
                // video fields
                e.Property(x => x.VideoFile).IsRequired(false);
                e.Property(x => x.VideoInfoFile).IsRequired(false);
                e.Property(x => x.VideoImageFile).IsRequired(false);
                e.Property(x => x.VideoStartOffsetSeconds).IsRequired(false);

                e.HasOne(x => x.Meet)
                 .WithMany()
                 .HasForeignKey(x => x.MeetId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Optional: basic check constraints for enums (SQLite)
                e.HasCheckConstraint("CK_Events_TrackType", "TrackType IN (0,1,2,3,4,100)");
                e.HasCheckConstraint("CK_Events_Gender", "Gender IN (0,1,2,100)");
                e.HasCheckConstraint("CK_Events_AgeGrouping", "AgeGrouping IN (0,1,2,100)");
            });

            // Results
            modelBuilder.Entity<Result>(e =>
            {
                e.ToTable("Results");
                e.HasKey(x => x.Id);
                e.Property(x => x.Lane).IsRequired(false);
                e.Property(x => x.BibNumber).IsRequired(false);
                e.Property(x => x.Name).IsRequired(false);
                e.Property(x => x.ResultSeconds).IsRequired(false);

                e.HasOne(x => x.Event)
                 .WithMany()
                 .HasForeignKey(x => x.EventId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
