using System.ComponentModel.DataAnnotations.Schema;

namespace AthStitcher.Data
{
    public class Result
    {
        public Result() 
        { 
        }
        public int Id { get; set; }
        public int HeatId { get; set; }

        [ForeignKey(nameof(HeatId))]
        public Heat? Heat { get; set; }

        public int? Lane { get; set; }
        public int? BibNumber { get; set; }
        public string? Name { get; set; }
        public double? ResultSeconds { get; set; }
    }
}
