using AthStitcher.Data;
using System;
using System.Globalization;
using System.ComponentModel;
using System.Xml.Linq;

using Instances;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
    public virtual Heat? Heat { get; set; }

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

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Display))]
    private string? club = string.Empty;

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

    public override string ToString()
    {
        string result = $"{Lane}: {ResultStr} {BibNumberStr} {Name} {Club}"; 
        return result;
    }


    public string ToCSV()
    {
        string result = $"{Lane},{ResultStr},{BibNumberStr},{Name},{Club}";
        return result;
    }

    public static string CSVHeader()
    {
        return "Lane,Result,BibNumber,Name";
    }

    public string ToTab()
    {
        string result = $"{Lane}\t{ResultStr}\t{BibNumberStr}\t{Name}\tClub";
        return result;
    }

    public static string TabHeader()
    {
        return "Lane\tResult\tBibNumber\tName";
    }


    [JsonIgnore]
    public string Display => ToString();

    [property: JsonIgnore]
    [property: NotMapped]
    [ObservableProperty] private bool isDirty = false;

    partial void OnLaneChanged(int? _, int? __) => IsDirty = true;
    partial void OnBibNumberChanged(int? _, int? __) => IsDirty = true;
    partial void OnNameChanged(string? _, string? __) => IsDirty = true;
    partial void OnResultSecondsChanged(double? _, double? __) => IsDirty = true;
}
