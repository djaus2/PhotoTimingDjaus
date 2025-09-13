using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PhotoTimingDjaus.Enums;

namespace AthStitcherGUI.Converters
{
    public class TimeFromIsFromGunFlashToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeFromMode mode)
            {
                return mode == TimeFromMode.FromGunFlash ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
