using Microsoft.EntityFrameworkCore.Update;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Windows.Controls;

namespace AthStitcher.Data
{
    public class Heat
    {
        public Heat()
        {
            Results = new List<Result>();
        }
     
        public int Id { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int HeatNo { get; set; }

        [JsonIgnore]
        [InverseProperty(nameof(Result.Heat))]
        public virtual ICollection<Result> Results { get; set; } = new List<Result>();

        public override string ToString()
        {
            if(HeatNo>0)
            {
                return $"Heat:{HeatNo}";
            }
            return "";
        }
    }
}
