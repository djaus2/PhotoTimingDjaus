using AthStitcher.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AthStitcher.Data
{

    public partial class Meet :ObservableObject
    {
        [Key]
        public int Id { get; set; }

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private string description;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public int round;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public DateTime? date;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public string? location = "";

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        public int? maxLanes  = 8;

        [JsonIgnore]
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();

        [JsonIgnore]
        // Convenience: date-only string for UI bindings
        public string DateStr => Date?.ToString("yyyy-MM-dd") ?? string.Empty;

        [JsonIgnore]
        [NotMapped]
        public string Display => ToString();

        public override string ToString()
        {
            string result = $"Meet Round:{Round}. Series:{Description} {DateStr} at {Location}";
            return result ;
        }
    }

}
