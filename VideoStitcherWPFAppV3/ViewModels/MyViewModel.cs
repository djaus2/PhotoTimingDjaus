using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using PhotoTimingDjaus; // This is where TimeFromMode is defined

namespace PhotoTimingGui.ViewModels
{
    public class MyViewModel : INotifyPropertyChanged
    {
        private Visibility _myVisibility = Visibility.Visible;
        private TimeFromMode _TimeFromMode;

        public Visibility MyVisibility
        {
            get => _myVisibility;
            set
            {
                _myVisibility = value;
                OnPropertyChanged(nameof(MyVisibility));
            }
        }

        public TimeFromMode TimeFromMode
        {
            get =>  _TimeFromMode;
            set
            {
                _TimeFromMode = value;
                OnPropertyChanged(nameof(TimeFromMode));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EnumToBooleanConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
