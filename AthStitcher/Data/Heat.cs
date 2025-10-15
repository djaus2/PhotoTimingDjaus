using System.ComponentModel.DataAnnotations.Schema;

namespace AthStitcher.Data
{
    public class Heat
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int HeatNo { get; set; }

        [NotMapped]
        public string Info
        {
            get
            {
                var e = Event;
                if (e == null)
                {
                    return $"Heat no:{HeatNo}";
                }
                string numPart = e.EventNumber.HasValue ? $"{e.EventNumber.Value}. " : string.Empty;
                string desc = e.Description ?? string.Empty;
                string age = e.AgeGrouping.ToString();
                string time = e.TimeStr;
                return $"{time} Event: {numPart}. {desc} {age}  Heat:{HeatNo}";
            }
        }
    }
}
