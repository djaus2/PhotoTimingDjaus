// File: AthStitcher/Converters.cs
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using AthStitcher.Data; // adjust if Gender/UnderAgeGroup are defined elsewhere

namespace AthStitcherGUI.Converters
{
    public class UnderAgeGroupByGenderConverter : IValueConverter
    {
        public UnderAgeGroupByGenderConverter() { }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var gender = value is Gender g ? g : Gender.none;
            var all = Enum.GetValues(typeof(UnderAgeGroup)).Cast<UnderAgeGroup>();

            // TODO: Replace this with your real rules
            return gender switch
            {
                Gender.male => all.Where(x => x.ToString().StartsWith("M")).ToList(),
                Gender.female => all.Where(x => x.ToString().StartsWith("F")).ToList(),
                _ => all.ToList()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}