using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AthStitcher.Data
{

    public class Meet
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Round { get; set; }
        public DateTime? Date { get; set; }
        public string? Location { get; set; }

        // Convenience: date-only string for UI bindings
        public string DateStr => Date?.ToString("yyyy-MM-dd") ?? string.Empty;
    }

}
