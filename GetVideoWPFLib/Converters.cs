using System;
using System.Globalization;
using System.Windows.Data;

namespace GetVideoWPFLib
{
    /// <summary>
    /// Converts a boolean value to its inverse (true to false, false to true)
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }

    /// <summary>
    /// Converts a boolean value to a listening status string (true = "Listening", false = "Not Listening")
    /// </summary>
    public class BoolToListeningStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Listening" : "Not Listening";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue == "Listening";
            }
            return false;
        }
    }
}
