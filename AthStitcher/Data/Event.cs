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
