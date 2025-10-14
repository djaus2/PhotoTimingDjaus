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
        public DateTime? Date { get; set; }
        public string? Location { get; set; }
    }

}
