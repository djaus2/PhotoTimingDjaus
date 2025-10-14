namespace AthStitcher.Data
{
    public class Result
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int? Lane { get; set; }
        public int? BibNumber { get; set; }
        public string? Name { get; set; }
        public double? ResultSeconds { get; set; }
    }
}
