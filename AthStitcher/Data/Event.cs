using AthStitcher.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace AthStitcher.Data
{
    [Table("Events")]
    public partial class Event : ObservableObject
    {
        public Event()
        {
        }

        // ...

        [Key]
        public int Id { get; set; }
        public int MeetId { get; set; }
        [ForeignKey(nameof(MeetId))]
        public Meet? meet;


        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int? eventNumber;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private DateTime? time;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private string? description;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int? distance;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int? hurdleSteepleHeight;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private TrackType trackType;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private Gender gender;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private AgeGrouping ageGrouping;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private UnderAgeGroup? underAgeGroup;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private MastersAgeGroup? mastersAgeGroup;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        [property: NotMapped]
        private MaleMastersAgeGroup? maleMastersAgeGroup;


        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        [property: NotMapped]
        private FemaleMastersAgeGroup? femaleMastersAgeGroup;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private string? videoInfoFile;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private double? videoStartOffsetSeconds;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int? minLane;
        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int? maxLane;

        [JsonIgnore]
        [NotMapped]
        public virtual ICollection<Heat> Heats { get; set; } = new List<Heat>();

        [JsonIgnore]
        [NotMapped]
        public string Display => ToString();

        [JsonIgnore]
        [NotMapped]
        public string DisplayName => ToString();    
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

            switch (AgeGrouping)
            {
                case AgeGrouping.junior:
                    ageGroupingStr = $"{UnderAgeGroup}";
                    break;
                case AgeGrouping.masters:
                    ageGroupingStr = $"Masters {MastersAgeGroup}";
                    break;
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

        /// <summary>
        // For Masters there are two gender based age groups used in two gender based separate ComboBoxes.
        // Only the correct ComboBox for the gender shows.
        // This next call translates the selection into the MastersAgeGroup property
        // There are separate enum lists for all three.
        // MaleMastersAgeGroup || FemaleMastersAgeGroup --> (Parsed as Text) --> MastersAgeGroup
        /// </summary>
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
