using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoTimingGui.ViewModels
{
    public class MyViewModel : INotifyPropertyChanged
    {
        private Visibility _myVisibility = Visibility.Visible;

        public Visibility MyVisibility
        {
            get => _myVisibility;
            set
            {
                _myVisibility = value;
                OnPropertyChanged(nameof(MyVisibility));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
