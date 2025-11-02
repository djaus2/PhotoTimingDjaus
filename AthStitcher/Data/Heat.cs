using AthStitcher.Data;
using System;
using System.Globalization;
using System.ComponentModel;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


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
        public virtual Event? Event { get; set; }

        [JsonIgnore]
        public virtual ICollection<LaneResult> Results { get; set; } = new List<LaneResult>();



        [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
        private int heatNo;



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

        [property: JsonIgnore]
        [property: NotMapped]
        [ObservableProperty] private bool isDirty=false;

        partial void OnHeatNoChanged(int _, int __) => IsDirty = true;

        public bool IsHeatDirty()
        {
            if(IsDirty) return true;
            foreach (var r in Results)
            {
                if (r.IsDirty) return true;
            }
            return false;
        }

    }
}
