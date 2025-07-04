using PhotoTimingDjaus.Enums;
using PhotoTimingGui.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;

namespace AthStitcher.ViewModels
{
    internal class AthStitcherViewModel
    {
        internal AthStitcherModel DataContext { get; set; } = new AthStitcherModel();

        internal AthStitcherViewModel( )
        {

        }

        public void GetAddr(object obj)
        {
            GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
            nint address = GCHandle.ToIntPtr(handle);

            System.Diagnostics.Debug.WriteLine($"Address: {address}");

            handle.Free();
        }

        /// <summary>
        /// Save the properties of the ViewModel to application settings.
        /// Automatically called on property change via PropertyChanged event.
        /// Changes saved after 1 second timeout
        /// </summary>
        public void SaveViewModel()
        {
            //MyViewModel viewModel = (this.DataContext as MyViewModel) ?? new MyViewModel(); // Ensure viewModel is not null, otherwise create a new instance
            string json = JsonSerializer.Serialize(DataContext);// viewModel);
            Properties.Settings.Default.SavedViewModel = json;
            Properties.Settings.Default.Save(); // Persist settings
        }

        /// <summary>
        /// Load the ViewModel from saved settings at startup.
        /// </summary>
        public AthStitcherModel LoadViewModel()
        {
            string json = Properties.Settings.Default.SavedViewModel;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    DataContext = JsonSerializer.Deserialize<AthStitcherModel>(json) ?? new AthStitcherModel();
                }
                catch (Exception)
                {
                    /// If in error reset
                    DataContext = new AthStitcherModel();
                }
            }
            else
            {
                DataContext = new AthStitcherModel();
            }
            if (DataContext is AthStitcherModel viewModel)
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
            return DataContext; // Return the ViewModel instance
        }

        public void SetSelectedStartTime(double selectedStartTime)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                //selectedStartTime = viewModel.StartTimeInput; // Get the current start time from the ViewModel

                viewModel.StartTimeInput = selectedStartTime;
            }
        }

        public double GetSelectedStartTime()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                double selectedStartTime = viewModel.StartTimeInput; // Get the current start time from the ViewModel

                return selectedStartTime;
            }
            return 0.0; // Default value if DataContext is not set or StartTimeInput is not available
        }

        public void SetHasStitched()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.HasStitched = true;
                //viewModel.HaveSelectedandShownGunLineToManualorWallClockMode = false;
            }
        }

        public bool Get_HasStitched()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                if (viewModel.HasStitched)
                    return true;
            }
            return false; // Default value if DataContext is not set or HasStitched is not available
        }

        public bool GetlevelImage()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                if (viewModel.ShowLevelImage)
                    return true;
            }
            return false; // Default value if DataContext is not set or ShowLevelImage is not available
        }

        public bool SetlevelImage(bool showLevelImage)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.ShowLevelImage = showLevelImage;
                return true; // Successfully set the state
            }
            return false; // Failed to set the state, DataContext is not available
        }

        public bool ManuallySelectMode()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                if (viewModel.TimeFromMode == TimeFromMode.ManuallySelect)
                    return true;
            }
            return false;
        }

        public bool HasSelectedandShownGunLineToManualMode()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                if (viewModel.HaveSelectedandShownGunLineToManualorWallClockMode)
                    return true;
            }
            return false; // Default value if DataContext is not set or HaveSelectedandShownGunLineToManualMode is not available
        }

        public bool IsDataContext()
        {
            return DataContext is AthStitcherModel;
        }

        public void SetVideoPath(string videoPath)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.VideoPathInput = videoPath;
            }
        }

        public string GetVideoPath()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return viewModel.VideoPathInput; // Get the current video path from the ViewModel
            }
            return string.Empty; // Default value if DataContext is not set or VideoPathInput is not available
        }

        public void SetEventWallClockStart(DateTime start)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.EventStartWallClockDateTime = start;
            }
        }

        public DateTime GetEventWallClockStart()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return viewModel.EventStartWallClockDateTime; // Get the current video path from the ViewModel
            }
            return DateTime.MinValue; // Default value if DataContext is not set or VideoPathInput is not available
        }

        public void SetEventWallClockStartTimeofDay(TimeSpan ts)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                string tsStr = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:d3}";
                viewModel.EventStartWallClockTimeofDay = tsStr;
            }
        }

        public TimeSpan GetEventWallClockStartTimeofDay()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return TimeSpan.Parse(viewModel.EventStartWallClockTimeofDay); // Get the current video path from the ViewModel
            }
            return TimeSpan.Zero; // Default value if DataContext is not set or VideoPathInput is not available
        }

        public void SetOutputPath(string outputPath)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.OutputPathInput = outputPath;
            }
        }

        public string GetOutputPath()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return viewModel.OutputPathInput; // Get the current output path from the ViewModel
            }
            return string.Empty; // Default value if DataContext is not set or OutputPathInput is not available
        }

        public void SetGunAudioPath(string GunAudioPathInput)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.GunAudioPathInput = GunAudioPathInput;
            }
        }

        public string GetGunAudioPath()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return viewModel.GunAudioPathInput; // Get the current gun audio path from the ViewModel
            }
            return string.Empty; // Default value if DataContext is not set or GunAudioPathInput is not available
        }

        // Fix for CS8121: Correcting the type in the pattern matching checks
        internal void SetMyVisibility(Visibility visibility)
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                viewModel.MyVisibility = visibility;
            }
        }

        // Method to access the ViewModel and get the MyVisibility property
        internal Visibility GetMyVisibility()
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                return viewModel.MyVisibility;
            }
            return Visibility.Visible; // Default value if ViewModel is not available
        }

        // Method to access the ViewModel and set the TimeFromMode property
        internal void SetTimeFromMode(TimeFromMode timeFromMode)
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                viewModel.TimeFromMode = timeFromMode;
            }
        }

        // Method to access the ViewModel and get the TimeFromMode property
        internal TimeFromMode GetTimeFromMode()
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                return viewModel.TimeFromMode;
            }
            return TimeFromMode.FromVideoStart; // Default value if ViewModel is not available
        }


        internal void SetVideoDetectMode(VideoDetectMode videoDetectMode)
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                viewModel.VideoDetectMode = videoDetectMode;
            }
        }

        // Method to access the ViewModel and get the TimeFromMode property
        internal VideoDetectMode GetVideoDetectMode()
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                return viewModel.VideoDetectMode;
            }
            return VideoDetectMode.FromFlash; // Default value if ViewModel is not available
        }

        internal void Set_HaveSelectedandShownGunLineinManualorWallClockMode(bool state)
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                viewModel.HaveSelectedandShownGunLineToManualorWallClockMode = state;
                return; // Successfully set the state
            }
            return; // Failed to set the state, DataContext is not available
        }

        internal bool Get_HaveSelectedandShownGunLineinManualorWallClockMode()
        {
            if (DataContext is AthStitcherModel viewModel) // Corrected type from AthStitcherModel to AthStitcherModel
            {
                if (viewModel.TimeFromMode != TimeFromMode.ManuallySelect &&
                    viewModel.TimeFromMode != TimeFromMode.WallClockSelect)
                    return true;
                return viewModel.HaveSelectedandShownGunLineToManualorWallClockMode;
            }
            return false;
        }

        internal void SetGunColor(OpenCvSharp.Scalar gunColor)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.GunColor = gunColor;
            }
        }

        internal OpenCvSharp.Scalar GetGunColor()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return viewModel.GunColor; // Get the current gun color from the ViewModel
            }
            return new OpenCvSharp.Scalar(255, 255, 255, 1); // Default color White if DataContext is not set or GunColor is not available
        }

        internal void SetVideoLength(double videoLength)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                viewModel.VideoLength = videoLength;
            }
        }

        internal double GetVideoLength()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                double videoLength = viewModel.VideoLength;
                return videoLength;
            }
            return 0;
        }

        internal void SetGunTime(double guntime, int gunTimeIndex)
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                // Gun time can be before video starts for WallClock mode
                var mode = viewModel.TimeFromMode;
                if ((guntime >= 0 || mode == TimeFromMode.WallClockSelect) && guntime < viewModel.VideoLength)
                {
                    viewModel.GunTime = guntime;
                    viewModel.GunTimeIndex = gunTimeIndex; // Set the index of the gun time
                }
            }
        }
        internal double GetGunTime()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                var gunTime = viewModel.GunTime;
                return gunTime;
            }
            return 0;
        }

        internal int GetGunTimeIndex()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                var gunTimeIndex = viewModel.GunTimeIndex;
                return gunTimeIndex;
            }
            return 0;
        }

        internal int GetMinPopupHeight()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                var minPopupHeight = viewModel.MinPopupHeight;
                return minPopupHeight;
            }
            return 0;
        }

        internal int GetMinPopupWidth()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                var minPopupWidth = viewModel.MinPopupWidth;
                return minPopupWidth;
            }
            return 0;
        }

        internal Thickness GetTimeLabelMargin()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
                return viewModel.TimeLabelMargin;
            }
            return new Thickness(0); // Default value if DataContext is not set or Thickness is not available
        }

        internal bool GetShowVideoFramePopup()
        {
            if (DataContext is AthStitcherModel viewModel)
            {
 
                bool show = viewModel.ShowVideoFramePopup;
                return show;
            }
            return false; // Default value if DataContext is not set or ShowVideoFramePopup is not available
        }

        internal void SetVideoCreationDate(DateTime? creationDate)
        {
            if (DataContext is AthStitcherModel viewModel)
            {

                viewModel. VideoCreationDate = creationDate?? DateTime.MaxValue;
            }
        }

        internal DateTime GetVideoCreationDate()
        {
            if (DataContext is AthStitcherModel viewModel)
            {

                return viewModel.VideoCreationDate;
            }
            return DateTime.MinValue;
        }

        internal string GetVideoCreationDateStr()
        {
            if (DataContext is AthStitcherModel viewModel)
            {

                DateTime dat =  viewModel.VideoCreationDate;
                return dat.ToString("yyyy-MM-dd HH:mm:ss.fff");
            }
            return "";
        }
        //SetEventWallClockStartTime
        internal void SetEventWallClockStartTime(DateTime start)
        {
            if (DataContext is AthStitcherModel viewModel)
            {

                viewModel.EventStartWallClockDateTime = start;
            }
        }

        internal DateTime GetEventWallClockStartTime()
        {
            if (DataContext is AthStitcherModel viewModel)
            {

                return viewModel.EventStartWallClockDateTime;
            }
            return DateTime.MinValue;
        }


    }
}
