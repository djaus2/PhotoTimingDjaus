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
using static PhotoTimingGui.ViewModels.enums;
using System.Windows.Controls.Primitives;

namespace PhotoTimingGui.ViewModels
{
    public class enums
    {
        public enum NudgeFrameLocation
        {
            Left,
            Center,
            Right
        }

    }
    public class AthStitcherModel : INotifyPropertyChanged
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
            set { _VideoPathInput = value; OnPropertyChanged(nameof(VideoPathInput)); } }

        private string _OutputPathInput = "";
        public string OutputPathInput { get => _OutputPathInput; set { _OutputPathInput = value; OnPropertyChanged(nameof(OutputPathInput)); } }

        private string _GunAudioPathInput = "";
        public string GunAudioPathInput { get => _GunAudioPathInput; set { _GunAudioPathInput = value; OnPropertyChanged(nameof(OutputPathInput)); } }

        public double _StartTimeInput = 0.0;
        public double StartTimeInput
        {
            get => _StartTimeInput;
            set { _StartTimeInput = value; OnPropertyChanged(nameof(StartTimeInput)); }
        }

        private double _VideoLength = 0;
        public double VideoLength
        {
            get => _VideoLength;
            set { _VideoLength = value; OnPropertyChanged(nameof(VideoLength)); }
        }

        private double _GunTime = 0;
        public double GunTime
        {
            get => _GunTime;
            set { _GunTime = value; OnPropertyChanged(nameof(GunTime)); }
        }

        private int _PopupVideoFrameImageX = 0;
        public int PopupVideoFrameImageX
        {
            get => _PopupVideoFrameImageX;
            set { _PopupVideoFrameImageX = value; OnPropertyChanged(nameof(PopupVideoFrameImageX)); }
        }

        private int _PopupVideoFrameImageY = 0;
        public int PopupVideoFrameImageY
        {
            get => _PopupVideoFrameImageY;
            set { _PopupVideoFrameImageY = value; OnPropertyChanged(nameof(PopupVideoFrameImageY)); }
        }

        private int _GunTimeIndex = 0;
        public int GunTimeIndex
        {
            get => _GunTimeIndex;
            set { _GunTimeIndex = value; OnPropertyChanged(nameof(GunTimeIndex)); }
        }

        private bool _ShowLevelImage = false;
        public bool ShowLevelImage
        {
            get => _ShowLevelImage;
            set { _ShowLevelImage = value; OnPropertyChanged(nameof(ShowLevelImage)); }
        }


        private bool _ShowVideoFramePopup = true;
        public bool ShowVideoFramePopup
        {
            get => _ShowVideoFramePopup;
            set { _ShowVideoFramePopup = value; OnPropertyChanged(nameof(ShowVideoFramePopup)); }
        }


        private int _PopupHeight = 150;
        public int MinPopupHeight
        {
            get => _PopupHeight;
            set { _PopupHeight = value; OnPropertyChanged(nameof(MinPopupHeight)); }
        }

        private int _PopupWidth = 150;
        public int MinPopupWidth
        {
            get => _PopupWidth;
            set { _PopupWidth = value;
                OnPropertyChanged(nameof(MinPopupWidth));
                OnPropertyChanged(nameof(PopupWidthHorizonatlOffset));
            }
        }

        public int PopupWidthHorizonatlOffset
        {
            get { return _PopupWidth / 2; }
        }

        public AthStitcherModel()
        {
            _setColorCommand = new RelayCommand(SetColor);
        }

        public bool HasNotStitched => !_HasStitched; // Inverse of HasStitched

        private bool _HasStitched = false;
        public bool HasStitched
        {
            get => _HasStitched;
            set
            {
                _HasStitched = value;
                OnPropertyChanged(nameof(HasStitched));
                OnPropertyChanged(nameof(HasNotStitched));
                //HaveSelectedandShownGunLineToManualorWallClockMode = false;
            }
        }

        private bool _HasSelectedandShownGunLineToManualMode = false;
        public bool HaveSelectedandShownGunLineToManualorWallClockMode
        {
            get => _HasSelectedandShownGunLineToManualMode;
            set
            {
                if ((TimeFromMode != TimeFromMode.ManuallySelect) &&
                    (TimeFromMode != TimeFromMode.WallClockSelect))
                    return;
                _HasSelectedandShownGunLineToManualMode = value; // Set HasStitched to true when switching to manual mode
                OnPropertyChanged(nameof(HaveSelectedandShownGunLineToManualorWallClockMode));
            }
        }


        public DateTime EventStartWallClockDateOnly
        {
            get
            {
                DateTime date = EventStartWallClockDateTime.Date;
                return date;
            }
            set
            {
                DateTime originalDateTime = EventStartWallClockDateTime;  // Example: 2025-06-16 13:45:30
                DateTime newDate = value; // Set to Christmas Day
                EventStartWallClockDateTime = newDate.Date + originalDateTime.TimeOfDay;
            }
        }


        public string EventStartWallClockTimeofDay
        {
            get
            {
                TimeSpan ts = _EventStartWallClockDateTime.TimeOfDay;
                string tsStr = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:d3}"; // Format as HH:MM:SS.mmm
                return tsStr;
            }

            set
            {
                if (TimeSpan.TryParse(value, out TimeSpan ts))
                {
                    EventStartWallClockDateTime = EventStartWallClockDateTime.Date + ts;
                }
                //Ignore change if not valid time format
                else
                {
                    MessageBox.Show("Invalid Time of Day", "Error", MessageBoxButton.OK);// Optionally, you can handle invalid time format here, e.g., show a message or log an error.
                }
            }
        }

        private DateTime _EventStartWallClockDateTime = DateTime.MinValue;
        public DateTime EventStartWallClockDateTime
        {
            get
            {
                DateTime dt = _EventStartWallClockDateTime;
                return dt;
            }
            set
            {
                _EventStartWallClockDateTime = value;
                OnPropertyChanged(nameof(EventStartWallClockDateTime));
                OnPropertyChanged(nameof(EventStartWallClockTimeofDay));
                OnPropertyChanged(nameof(EventStartWallClockDateOnly));
            }
        }

        private DateTime _VideoCreationDate;
        public DateTime VideoCreationDate
        {
            get => _VideoCreationDate;
            set
            {
                _VideoCreationDate = value;
                OnPropertyChanged(nameof(VideoCreationDate));
            }
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
            get => _TimeFromMode;
            set
            {
                _TimeFromMode = value;
                OnPropertyChanged(nameof(TimeFromMode));
                HasStitched = false; // Reset HasStitched when TimeFromMode changes
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

        private Thickness _TimeLabeMargin = new Thickness(10, 100, 0, 0);

        public Thickness TimeLabelMargin
        {
            get => _TimeLabeMargin;
            set
            {
                _TimeLabeMargin = value;
                OnPropertyChanged(nameof(TimeLabelMargin));
            }
        }

        /*
        private bool _isPopupVisible;

        public bool IsPopupVisible
        {
            get => _isPopupVisible;
            set
            {
                _isPopupVisible = value;
                OnPropertyChanged(nameof(IsPopupVisible));
            }
        }*/

        public Visibility ShowVideoFramePopupCheckbox => HasStitched ? Visibility.Visible : Visibility.Collapsed;

        /*
        private BOO bShow_ShowVideoFramePopupCheckbox()
        {
                bool stitched = HasStitched;
                if (!stitched)
                {
                    IsPopupVisible = false;
                }// Do not show checkbox if not stitched and gun line has not been selected
                var mode = TimeFromMode;
                if (mode == TimeFromMode.ManuallySelect) && (!stitched))
            //return Visibility.Collapsed;
            IsIsPopupVisible ;
        }*/



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
        public bool ManualSelected => TimeFromMode.Equals(_TimeFromMode, TimeFromMode.ManuallySelect);

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

        public Thickness TimeLabeMargin { get => _TimeLabeMargin; set => _TimeLabeMargin = value; }

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



            private PlacementMode _placement = PlacementMode.Bottom;
            public PlacementMode PopupPlacement
            {
                get => _placement;
                set
                {
                    if (_placement != value)
                    {
                        _placement = value;
                        OnPropertyChanged(nameof(PopupPlacement));
                    }
                }
            }

            // Include INotifyPropertyChanged implementation...
    
        /*
        public string NudgePlacement
        {
            get
            {
                return NudgeLocation switch
                {
                    NudgeFrameLocation.Left => "Left",
                    NudgeFrameLocation.Center => "Center",
                    NudgeFrameLocation.Right => "Right",
                    _ => "Unknown"
                };
            }
        }*/
        //public ICommand NudgeLeftCommand => new RelayCommand(_ => NudgeFrame(NudgeFrameLocation.Left));

        // INotifyPropertyChanged implementation



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

    public class PlacementModeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlacementMode mode && parameter is string param)
            {
                return mode.ToString().Equals(param, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && parameter is string param &&
                Enum.TryParse(typeof(PlacementMode), param, true, out var result))
            {
                return result;
            }
            return Binding.DoNothing;
        }

    }



}
