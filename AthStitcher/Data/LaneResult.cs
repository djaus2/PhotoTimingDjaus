using AthStitcher.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Linq;

[Table("Results")]
public partial class LaneResult : ObservableObject
{
    public LaneResult()
    {

    }

    [Key]
    public int Id { get; set; }
    public int HeatId { get; set; }
    [ForeignKey(nameof(HeatId))]
    public Heat? Heat { get; set; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)), NotifyPropertyChangedFor(nameof(LaneStr))]
    private int? lane;

    [JsonIgnore]
    [NotMapped]
    public string LaneStr
    {
        get => $"Lane {Lane}";
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)), NotifyPropertyChangedFor(nameof(ResultStr))]
    private double? resultSeconds = 0.0;

    [JsonIgnore]
    [NotMapped]
    public string ResultStr
    {
        get
        {
            if (ResultSeconds == null)
                return "";
            else if (ResultSeconds <= 0.0)
                return "";
            else
                return ResultSeconds?.ToString("F3");
        }
        set
        {
            if (double.TryParse(value, out double parsedValue))
            {
                ResultSeconds = parsedValue;
            }
            else
            {
                ResultSeconds = null ; // or handle invalid input as needed
            }
        }
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)), NotifyPropertyChangedFor(nameof(BibNumberStr))]
    private int? bibNumber = 0;

    [JsonIgnore]
    [NotMapped]
    public string BibNumberStr
    {
        get
        {
            if (BibNumber == null)
                return "";
            else if (BibNumber <= 0)
                return "";
            else
                return BibNumber?.ToString("F0");
        }
        set
        {
            if (int.TryParse(value, out int parsedValue))
            {
                BibNumber = parsedValue;
            }
            else
            {
                BibNumber = null; // or handle invalid input as needed
            }
        }
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display)), NotifyPropertyChangedFor(nameof(NameStr))]
    private string? name = string.Empty;

    [JsonIgnore]
    [NotMapped]
    public string NameStr
    {
        get
        {
            if (string.IsNullOrEmpty(Name)) 
                return "";
            else
                return Name;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Name = "";
            }
            else
            {
                Name = value;
            }
        }
    }

    [JsonIgnore]
    public string Display => ToString();
}
