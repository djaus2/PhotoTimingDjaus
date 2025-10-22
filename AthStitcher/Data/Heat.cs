using System.ComponentModel.DataAnnotations.Schema;

namespace AthStitcher.Data
{
    public class Heat
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int HeatNo { get; set; }

        public override string ToString()
        {
            //var e = Event;
            //if (e == null)
            //{
            //    return $"";
            //}
            if(HeatNo>0)
            {
                return $"Heat:{HeatNo}";
            }
            return "";
        }
    }
}
