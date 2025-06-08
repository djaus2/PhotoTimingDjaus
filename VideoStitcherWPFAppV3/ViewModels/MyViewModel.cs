using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PhotoTimingDjaus;
using PhotoTimingDjaus.Enums;// This is where TimeFromMode is defined
using OpenCvSharp;
using System.Text.Json;

namespace PhotoTimingGui.ViewModels
{
    public class MyViewModel : INotifyPropertyChanged
    {
        private Scalar _gunColor = new Scalar(255, 255, 255, 1); // Default to white
        private ICommand _setColorCommand;
        private string _selectedColorName = "Black"; // Default color name
        private string _selectedColorBackgroundName = "Gray"; //Normal is white, except for White SelectedColor, for contrast.
        private Visibility _myVisibility = Visibility.Visible;
        private TimeFromMode _TimeFromMode;
        private VideoDetectMode _videoDetectMode = VideoDetectMode.FromFlash;

        private string _VideoPathInput = "";
        public string VideoPathInput { get => _VideoPathInput; 
            set  { _VideoPathInput = value; OnPropertyChanged(nameof(VideoPathInput)); } }

        private string _OutputPathInput = "";
        public string OutputPathInput { get => _OutputPathInput; set  { _OutputPathInput = value; OnPropertyChanged(nameof(OutputPathInput)); } }

        private string _GunAudioPathInput = "";
        public string GunAudioPathInput { get => _GunAudioPathInput; set  { _GunAudioPathInput = value; OnPropertyChanged(nameof(OutputPathInput)); } }



        public MyViewModel()
        {
            _setColorCommand = new RelayCommand(SetColor);
        }

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

        public VideoDetectMode VideoDetectMode
        {
            get => _videoDetectMode;
            set
            {
                _videoDetectMode = value;
                OnPropertyChanged(nameof(VideoDetectMode));
            }
        }

        public Scalar GunColor
        {
            get => _gunColor;
            set
            {
                _gunColor = value;
                OnPropertyChanged(nameof(GunColor));
            }
        }

        // Selected color name property
        public string SelectedColorName
        {
            get => _selectedColorName;
            set
            {
                if (_selectedColorName != value)
                {
                    _selectedColorName = value;
                    OnPropertyChanged(nameof(SelectedColorName));
                    // Update all IsColorSelected properties
                    UpdateColorSelectionProperties();
                    {
                        string bgvalue = "White";
                        if (_selectedColorName == "White")
                            bgvalue = "Black";
                        SelectedColorBackgroundName = bgvalue;
                    }
                }
            }
        }

        public string SelectedColorBackgroundName
        {
            get => _selectedColorBackgroundName;
            set
            {
                if (_selectedColorBackgroundName != value)
                {
                    _selectedColorBackgroundName = value;
                    OnPropertyChanged(nameof(SelectedColorBackgroundName));
                    UpdateColorSelectionProperties();
                }
            }
        }

        // Color selection indicator properties
        public bool IsRedSelected => string.Equals(_selectedColorName, "Red", StringComparison.OrdinalIgnoreCase);
        public bool IsGreenSelected => string.Equals(_selectedColorName, "Green", StringComparison.OrdinalIgnoreCase);
        public bool IsBlueSelected => string.Equals(_selectedColorName, "Blue", StringComparison.OrdinalIgnoreCase);
        public bool IsYellowSelected => string.Equals(_selectedColorName, "Yellow", StringComparison.OrdinalIgnoreCase);
        public bool IsCyanSelected => string.Equals(_selectedColorName, "Cyan", StringComparison.OrdinalIgnoreCase);
        public bool IsMagentaSelected => string.Equals(_selectedColorName, "Magenta", StringComparison.OrdinalIgnoreCase);
        public bool IsWhiteSelected => string.Equals(_selectedColorName, "White", StringComparison.OrdinalIgnoreCase);
        public bool IsBlackSelected => string.Equals(_selectedColorName, "Black", StringComparison.OrdinalIgnoreCase);      
        public bool FlashSelected => TimeFromMode.Equals(_TimeFromMode, TimeFromMode.FromGunViaVideo);

        private void UpdateColorSelectionProperties()
        {
            OnPropertyChanged(nameof(IsRedSelected));
            OnPropertyChanged(nameof(IsGreenSelected));
            OnPropertyChanged(nameof(IsBlueSelected));
            OnPropertyChanged(nameof(IsYellowSelected));
            OnPropertyChanged(nameof(IsCyanSelected));
            OnPropertyChanged(nameof(IsMagentaSelected));
            OnPropertyChanged(nameof(IsWhiteSelected));
            OnPropertyChanged(nameof(IsBlackSelected));
        }

        public ICommand SetColorCommand => _setColorCommand;

        private void SetColor(object parameter)
        {
            if (parameter is string colorName)
            {
                Scalar selectedColor;
                switch (colorName.ToUpper())
                {
                    case "RED":
                        selectedColor = new Scalar(0, 0, 255, 1);
                        break;
                    case "GREEN":
                        selectedColor = new Scalar(0, 255, 0, 1);
                        break;
                    case "BLUE":
                        selectedColor = new Scalar(255, 0, 0, 1);
                        break;
                    case "YELLOW":
                        selectedColor = new Scalar(0, 255, 255, 1);
                        break;
                    case "CYAN":
                        selectedColor = new Scalar(255, 255, 0, 1);
                        break;
                    case "MAGENTA":
                        selectedColor = new Scalar(255, 0, 255, 1);
                        break;
                    case "WHITE":
                        selectedColor = new Scalar(255, 255, 255, 1);
                        break;
                    case "BLACK":
                        selectedColor = new Scalar(0, 0, 0, 1);
                        break;
                    default:
                        return; // Invalid color
                }
                GunColor = selectedColor;
                SelectedColorName = colorName; // Update the selected color name
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
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
