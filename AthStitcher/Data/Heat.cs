using AthStitcher.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace AthStitcher.Data
{
    [Table("Heats")]
    public partial class Heat :ObservableObject
    {
        public Heat()
        {
        }

        [Key]
        public int Id { get; set; }
        public int EventId { get; set; }
        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }


        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int heatNo;

        [JsonIgnore]
        [NotMapped]
        public virtual ICollection<LaneResult> Results { get; set; } = new List<LaneResult>();


        public override string ToString()
        {
            if(HeatNo>0)
            {
                return $"Heat:{HeatNo}";
            }
            return "";
        }

        [JsonIgnore]
        [NotMapped]
        public string Display => ToString();
    }
}
