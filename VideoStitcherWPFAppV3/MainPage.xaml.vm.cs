using PhotoTimingDjaus.Enums;
using PhotoTimingGui.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;

namespace PhotoTimingGui
{
    public partial class MainWindow
    {
        /// <summary>
        /// Save the properties of the ViewModel to application settings.
        /// Automatically called on property change via PropertyChanged event.
        /// Changes saved after 1 second timeout
        /// </summary>
        public void SaveViewModel()
        {
            MyViewModel viewModel = (this.DataContext as MyViewModel) ?? new MyViewModel(); // Ensure viewModel is not null, otherwise create a new instance
            string json = JsonSerializer.Serialize(viewModel);
            VideoStitcherWPFAppV3.Properties.Settings.Default.SavedViewModel = json;
            VideoStitcherWPFAppV3.Properties.Settings.Default.Save(); // Persist settings
        }

        /// <summary>
        /// Load the ViewModel from saved settings at startup.
        /// </summary>
        public void LoadViewModel()
        {
            string json = VideoStitcherWPFAppV3.Properties.Settings.Default.SavedViewModel;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    this.DataContext = JsonSerializer.Deserialize<MyViewModel>(json) ?? new MyViewModel();
                }
                catch (Exception)
                {
                    /// If in error reset
                    this.DataContext = new MyViewModel();
                }
            }
            else
            {
                this.DataContext = new MyViewModel();
            }
            if (this.DataContext is MyViewModel viewModel)
            {
                // Set default visibility at start to visble for controls
                //Add any other defaults here.
                viewModel.GunTimeIndex = 0; // Reset the gun time index to 0 before saving
                viewModel.GunTime = 0.0; // Reset the gun time to 0 before saving
                viewModel.MyVisibility = Visibility.Visible;
                viewModel.StartTimeInput = 0.0; // Default start time
                viewModel.HasStitched = false;
                viewModel.HaveSelectedandShownGunLineToManualorWallClockMode = false;
                viewModel.GunColor = new OpenCvSharp.Scalar(255, 255, 255, 1); // Default gun color White
                viewModel.SelectedColorName = "White"; // Default color name
            }
        }

        public void SetSelectedStartTime(double selectedStartTime)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                //selectedStartTime = viewModel.StartTimeInput; // Get the current start time from the ViewModel

                viewModel.StartTimeInput = selectedStartTime;
            }
        }

        public double GetSelectedStartTime()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                double selectedStartTime = viewModel.StartTimeInput; // Get the current start time from the ViewModel

                return selectedStartTime;
            }
            return 0.0; // Default value if DataContext is not set or StartTimeInput is not available
        }

        public void SetHasStitched()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                viewModel.HasStitched = true;
                viewModel.HaveSelectedandShownGunLineToManualorWallClockMode = false;
            }
        }

        public bool Get_HasStitched()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                if (viewModel.HasStitched)
                    return true;
            }
            return false; // Default value if DataContext is not set or HasStitched is not available
        }

        public bool GetlevelImage()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                if (viewModel.ShowLevelImage)
                    return true;
            }
            return false; // Default value if DataContext is not set or ShowLevelImage is not available
        }

        public bool SetlevelImage(bool showLevelImage)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                viewModel.ShowLevelImage = showLevelImage;
                return true; // Successfully set the state
            }
            return false; // Failed to set the state, DataContext is not available
        }

        public bool ManuallySelectMode()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                if (viewModel.TimeFromMode == TimeFromMode.ManuallySelect)
                    return true;
            }
            return false;
        }

        public bool HasSelectedandShownGunLineToManualMode()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                if (viewModel.HaveSelectedandShownGunLineToManualorWallClockMode)
                    return true;
            }
            return false; // Default value if DataContext is not set or HaveSelectedandShownGunLineToManualMode is not available
        }

        public bool IsDataContext()
        {
            return this.DataContext is MyViewModel;
        }

        public void SetVideoPath(string videoPath)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                viewModel.VideoPathInput = videoPath;
            }
        }

        public string GetVideoPath()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                return viewModel.VideoPathInput; // Get the current video path from the ViewModel
            }
            return string.Empty; // Default value if DataContext is not set or VideoPathInput is not available
        }

        public void SetEventWallClockStart(DateTime start)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                viewModel.EventStartWallClockDateTime = start;
            }
        }

        public DateTime GetEventWallClockStart()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                return viewModel.EventStartWallClockDateTime; // Get the current video path from the ViewModel
            }
            return DateTime.MinValue; // Default value if DataContext is not set or VideoPathInput is not available
        }

        public void SetEventWallClockStartTimeofDay(TimeSpan ts)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                string tsStr = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:d3}";
                viewModel.EventStartWallClockTimeofDay = tsStr;
            }
        }

        public TimeSpan GetEventWallClockStartTimeofDay()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                return TimeSpan.Parse(viewModel.EventStartWallClockTimeofDay); // Get the current video path from the ViewModel
            }
            return TimeSpan.Zero; // Default value if DataContext is not set or VideoPathInput is not available
        }

        public void SetOutputPath(string outputPath)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                viewModel.OutputPathInput = outputPath;
            }
        }

        public string GetOutputPath()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                return viewModel.OutputPathInput; // Get the current output path from the ViewModel
            }
            return string.Empty; // Default value if DataContext is not set or OutputPathInput is not available
        }

        public void SetGunAudioPath(string GunAudioPathInput)
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                viewModel.GunAudioPathInput = GunAudioPathInput;
            }
        }

        public string GetGunAudioPath()
        {
            if (this.DataContext is MyViewModel viewModel)
            {
                return viewModel.GunAudioPathInput; // Get the current gun audio path from the ViewModel
            }
            return string.Empty; // Default value if DataContext is not set or GunAudioPathInput is not available
        }

        // Method to access the ViewModel and set the MyVisibility property
        private void SetMyVisibility(Visibility visibility)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                viewModel.MyVisibility = visibility;
            }
        }

        // Method to access the ViewModel and get the MyVisibility property
        private Visibility GetMyVisibility()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                return viewModel.MyVisibility;
            }
            return Visibility.Visible; // Default value if ViewModel is not available
        }

        // Method to access the ViewModel and set the TimeFromMode property
        private void SetTimeFromMode(TimeFromMode timeFromMode)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                viewModel.TimeFromMode = timeFromMode;
            }
        }

        // Method to access the ViewModel and get the TimeFromMode property
        private TimeFromMode GetTimeFromMode()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                return viewModel.TimeFromMode;
            }
            return TimeFromMode.FromButtonPress; // Default value if ViewModel is not available
        }


        private void SetVideoDetectMode(VideoDetectMode videoDetectMode)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                viewModel.VideoDetectMode = videoDetectMode;
            }
        }

        // Method to access the ViewModel and get the TimeFromMode property
        private VideoDetectMode GetVideoDetectMode()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                return viewModel.VideoDetectMode;
            }
            return VideoDetectMode.FromFlash; // Default value if ViewModel is not available
        }

        private void Set_HaveSelectedandShownGunLineinManualorWallClockMode(bool state)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                viewModel.HaveSelectedandShownGunLineToManualorWallClockMode = state;
                return; // Successfully set the state
            }
            return; // Failed to set the state, DataContext is not available
        }

        private bool Get_HaveSelectedandShownGunLineinManualorWallClockMode()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                if((viewModel.TimeFromMode!= TimeFromMode.ManuallySelect) &&
                        (viewModel.TimeFromMode != TimeFromMode.WallClockSelect))
                    return true;
                return viewModel.HaveSelectedandShownGunLineToManualorWallClockMode;
            }
            return false;

        }

        private void SetGunColor(OpenCvSharp.Scalar gunColor)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                viewModel.GunColor = gunColor;
            }
        }

        private OpenCvSharp.Scalar GetGunColor()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                return viewModel.GunColor; // Get the current gun color from the ViewModel
            }
            return new OpenCvSharp.Scalar(255, 255, 255, 1); // Default color White if DataContext is not set or GunColor is not available
        }

        private void SetVideoLength(double videoLength)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                viewModel.VideoLength = videoLength;
            }
        }

        private double GetVideoLength()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                double videoLength = viewModel.VideoLength;
                return videoLength;
            }
            return 0;
        }

        private void SetGunTime(double guntime, int gunTimeIndex)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                // Gun time can be before video starts for WallClock mode
                var mode = viewModel.TimeFromMode;
                if (((guntime >= 0) || (mode == TimeFromMode.WallClockSelect)) && (guntime < viewModel.VideoLength))
                {
                    viewModel.GunTime = guntime;
                    viewModel.GunTimeIndex = gunTimeIndex; // Set the index of the gun time
                }
            }
        }
        private double GetGunTime()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                var gunTime = viewModel.GunTime;
                return gunTime;
            }
            return 0;
        }

        private int GetGunTimeIndex()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                var gunTimeIndex = viewModel.GunTimeIndex;
                return gunTimeIndex;
            }
            return 0;
        }

        private int GetMinPopupHeight()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                var minPopupHeight = viewModel.MinPopupHeight;
                return minPopupHeight;
            }
            return 0;
        }

        private int GetMinPopupWidth()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                var minPopupWidth = viewModel.MinPopupWidth;
                return minPopupWidth;
            }
            return 0;
        }

        private Thickness GetTimeLabelMargin()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
                return viewModel.TimeLabelMargin;
            }
            return new Thickness(0); // Default value if DataContext is not set or Thickness is not available
        }

        private bool GetShowVideoFramePopup()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {
 
                bool show = viewModel.ShowVideoFramePopup;
                return show;
            }
            return false; // Default value if DataContext is not set or ShowVideoFramePopup is not available
        }

        private void SetVideoCreationDate(DateTime? creationDate)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {

                viewModel. VideoCreationDate = creationDate?? DateTime.MaxValue;
            }
        }

        private DateTime GetVideoCreationDate()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {

                return viewModel.VideoCreationDate;
            }
            return DateTime.MinValue;
        }
        //SetEventWallClockStartTime
        private void SetEventWallClockStartTime(DateTime start)
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {

                viewModel.EventStartWallClockDateTime = start;
            }
        }

        private DateTime GetEventWallClockStartTime()
        {
            if (DataContext is ViewModels.MyViewModel viewModel)
            {

                return viewModel.EventStartWallClockDateTime;
            }
            return DateTime.MinValue;
        }


    }
}
