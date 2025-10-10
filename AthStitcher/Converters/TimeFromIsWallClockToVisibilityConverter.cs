using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Sportronics.VideoEnums;

namespace AthStitcherGUI.Converters
{
    public class TimeFromIsWallClockToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeFromMode mode)
            {
                return mode.ToString() == "WallClockSelect" ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
