using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace AthStitcher.Data
{
    public partial class Event : ObservableObject
    {
        public int Id;
        public int MeetId;

        [ObservableProperty]
        public Meet? meet;
        [ObservableProperty]
        public int? eventNumber;
        [ObservableProperty]
        public DateTime? time;
        [ObservableProperty]
        public string? description;
        [ObservableProperty]
        public int? distance;
        [ObservableProperty]
        public int? hurdleSteepleHeight;

        [ObservableProperty]
        public TrackType trackType;
        [ObservableProperty]
        public Gender gender;
        [ObservableProperty]
        public AgeGrouping ageGrouping;
        [ObservableProperty]
        public UnderAgeGroup? underAgeGroup;
        [ObservableProperty]
        public MastersAgeGroup? mastersAgeGroup;

        [ObservableProperty]
        [property: NotMapped]
        public MaleMastersAgeGroup? maleMastersAgeGroup;


        [ObservableProperty]
        [property: NotMapped]
        public FemaleMastersAgeGroup? femaleMastersAgeGroup;

        [ObservableProperty]
        public string? videoInfoFile;
        [ObservableProperty]
        public double? videoStartOffsetSeconds;
        [ObservableProperty]
        public int? minLane;
        [ObservableProperty]
        public int? maxLane;



        // Convenience: time-of-day string for UI bindings (12-hour with AM/PM)
        [NotMapped]
        public string TimeStr => Time?.ToString("h:mm tt", CultureInfo.CurrentCulture) ?? string.Empty;

        public override string ToString()
        {
            string genderStr;
            {
                var g = $"{Gender}" ?? "";
                var formattedGender = g.Length > 0
                    ? char.ToUpper(g[0], System.Globalization.CultureInfo.CurrentCulture) + g[1..].ToLower(System.Globalization.CultureInfo.CurrentCulture)
                    : g;
                genderStr = $"{formattedGender}";
            }
            string ageGroupingStr;
            {
                var ag = $"{AgeGrouping}" ?? "";
                var formattedAgeGroupingStr = ag.Length > 0
                    ? char.ToUpper(ag[0], System.Globalization.CultureInfo.CurrentCulture) + ag[1..].ToLower(System.Globalization.CultureInfo.CurrentCulture)
                    : ag;
                ageGroupingStr = $"{formattedAgeGroupingStr}";
            }
            string trackTypeStr;
            {
                var t = $"{TrackType}" ?? "";
                var formattedTrackTypeStr = t.Length > 0
                    ? char.ToUpper(t[0], System.Globalization.CultureInfo.CurrentCulture) + t[1..].ToLower(System.Globalization.CultureInfo.CurrentCulture)
                    : t;
                trackTypeStr = $"{formattedTrackTypeStr}";
            }

            string result = $"Event No:{EventNumber} {TimeStr} {genderStr} {ageGroupingStr} {Distance}m {trackTypeStr} {Description}";
            return result;
        }

        public void SetMastersAgeGenderGroup()
        {
            if (Gender == Gender.male)
            {
                if (MaleMastersAgeGroup != null)
                {
                    MastersAgeGroup = (MastersAgeGroup)Enum.Parse(typeof(MastersAgeGroup), MaleMastersAgeGroup.ToString());
                }
            }
            else if (Gender == Gender.female)
            {
                if (FemaleMastersAgeGroup != null)
                {
                    MastersAgeGroup = (MastersAgeGroup)Enum.Parse(typeof(MastersAgeGroup), FemaleMastersAgeGroup.ToString());
                }
            }
        }

        public void GetMastersAgeGenderGroup()
        {
            if (Gender == Gender.male)
            {
                if (MastersAgeGroup is not null &&
                    Enum.TryParse<MaleMastersAgeGroup>(MastersAgeGroup.ToString(), true, out var parsed))
                {
                    MaleMastersAgeGroup = parsed;
                }
                else
                {
                    MaleMastersAgeGroup = null; // or a specific fallback
                }
            }
            else if (Gender == Gender.female)
            {
                if (MastersAgeGroup is not null &&
                    Enum.TryParse<FemaleMastersAgeGroup>(MastersAgeGroup.ToString(), true, out var parsed))
                {
                    FemaleMastersAgeGroup = parsed;
                }
                else
                {
                    FemaleMastersAgeGroup = null; // or a specific fallback
                }
            }
        }
    }
}
