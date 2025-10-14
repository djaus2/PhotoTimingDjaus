using System;
using System.Globalization;

namespace AthStitcher.Data
{
    public class Event
    {
        public int Id { get; set; }
        public int MeetId { get; set; }
        public Meet? Meet { get; set; }

        public int? EventNumber { get; set; }
        public int? HeatNumber { get; set; }
        public DateTime? Time { get; set; }
        public string? Description { get; set; }
        public int? Distance { get; set; }
        public int? HurdleSteepleHeight { get; set; }

        public TrackType TrackType { get; set; } = TrackType.na;
        public Gender Gender { get; set; } = Gender.none;
        public AgeGrouping AgeGrouping { get; set; } = AgeGrouping.none;
        public UnderAgeGroup? UnderAgeGroup { get; set; }
        public MastersAgeGroup? MastersAgeGroup { get; set; }

        public string? VideoInfoFile { get; set; }
        public double? VideoStartOffsetSeconds { get; set; }

        // Convenience: time-of-day string for UI bindings (12-hour with AM/PM)
        public string TimeStr => Time?.ToString("h:mm tt", CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
