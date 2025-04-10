using OpenCvSharp;
using PhotoTimingDjaus;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading.Tasks;



namespace StitchInTime
{
    public partial class MainPage : ContentPage
    {

        string videoPathInit = @"C:\Users\david\OneDrive\Documents\Camtasia\MVPRenewal\MVPRenewal.mp4";
        string outputPathInit = @"c:\temp\vid\stitched_image67.png";
        int startTimeSecondsInit = 0;
        string videoPath { get => videoPathInit; set => videoPathInit = value; }
        string outputPath { get => outputPathInit; set => outputPathInit = value; }
        int startTimeSeconds { get => startTimeSecondsInit; set => startTimeSecondsInit = value; }

        int videoLength = 0;
        Image image;

        public class MyViewModel
        {
            private int _count;
            public int Count
            {
                get => _count;
                set
                {
                    _count = value;
                    OnPropertyChanged(nameof(Count)); // Notify the UI
                }
            }

            private bool _isBussy;
            public bool IsBussy
            {
                get => _isBussy;
                set
                {
                    _isBussy = value;
                    OnPropertyChanged(nameof(IsBussy)); // Notify the UI
                }
            }

            public MyViewModel()
            {
                IsBussy = false; // Initial value
                Count = 0;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public MainPage()
        {
            InitializeComponent();
            //SourceInfo.
            Entry vid = (Entry)FindByName("Source");
            vid.Text = videoPathInit;
            Entry stitch = (Entry)FindByName("Stitch");
            stitch.Text = outputPathInit;
            Entry start = (Entry)FindByName("Start");
            start.Text = $"{startTimeSecondsInit}";
            Entry vidLength = (Entry)FindByName("VidLength");
            vidLength.Text = $"{videoLength}";
            // Path to save the stitched image
            BindingContext = new MyViewModel();
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// In XAML:
        /// <VerticalStackLayout x:Name="MyLayout"
        //////////////////////////////////////////////////////////////////////////////////////
        /// ActivityIndicator is created and shown on Button press to start Stitching
        /// Is disposed when the stitching process is completed
        //////////////////////////////////////////////////////////////////////////////////////

        // The activity indicator
        private ActivityIndicator activityIndicator;

        /// <summary>
        /// Called by Buttun Click event handler on MainThread
        /// </summary>
        private void StartActivity()
        {
            // Start the activity indicator
            activityIndicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = Colors.Blue // Optional: Set a color for visibility
            };
            // Assuming you have a named layout in XAML
            MyLayout.Children.Add(activityIndicator);
        }

        /// <summary>
        /// Called by BackgroundWorker at worker.RunWorkerCompleted.
        /// Calls StopActivity on MainThread
        /// </summary>
        private void StoppActivity()
        {
            MainThread.InvokeOnMainThreadAsync(() => StopActivity());
        }

        /// <summary>
        /// Runs on MainThread to stop the activity indicator and remove it from the layout
        ///  </summary>
        private void StopActivity()
        {
            // Stop the activity indicator
            if (activityIndicator != null)
            {
                activityIndicator.IsRunning = false;
                MyLayout.Children.Remove(activityIndicator);
                Entry vidLength = (Entry)FindByName("VidLength");
                vidLength.Text = $"{videoLength}";
                if(image != null)
                {
                    MyLayout.Children.Remove(image);
                }
                image = new Image
                {
                    Source = ImageSource.FromFile(outputPath)
                };
                MyLayout.Children.Add(image);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////
        
        private void OnCounterClicked(object sender, EventArgs e)
        {

            StartActivity();
        
            if (BindingContext is MyViewModel viewModel)
            {
                MainThread.InvokeOnMainThreadAsync(() => viewModel.IsBussy = true);
            }
            CounterBtn.Text = $"Stitching";
            //StitchUp(videoPath, outputPath, startTimeSeconds);
            asyncStitchUp(videoPath, outputPath, startTimeSeconds).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // Handle any exceptions that occurred during the async operation
                    System.Diagnostics.Debug.WriteLine($"Stitchup Run Error: {t.Exception?.Message}");
                }
                else
                {
                    // Continue with the rest of your code after the async operation completes
                    System.Diagnostics.Debug.WriteLine("Async Stitchup operation completed successfully.");
                }
            });
            SemanticScreenReader.Announce(CounterBtn.Text);
        }



        private void OnEntryCompletedVideoPath(object sender, EventArgs e)
        {
            videoPath = ((Entry)sender).Text;
        }

        private void OnEntryCompletedStitchFilePath(object sender, EventArgs e)
        {
            outputPath = ((Entry)sender).Text;
        }

        private void Start_Completed(object sender, EventArgs e)
        {
            string val = ((Entry)sender).Text;
            if(int.TryParse(val, out int result))
            {
                videoLength = result;
            }
        }

        private void GetStitchUp(object sender, EventArgs e)
        {
            asyncGetStitchUp().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // Handle any exceptions that occurred during the async operation
                    System.Diagnostics.Debug.WriteLine($"GetStitchUp Files Error: {t.Exception?.Message}");
                }
                else
                {
                    // Continue with the rest of your code after the async operation completes
                    System.Diagnostics.Debug.WriteLine("Async operation GetStitchUp Files completed successfully.");
                }
            });
        }

        private async Task asyncGetStitchUp()
        {

            var result = await PickAndShow(true);
            if (result == null)
            {
                return;
            }
            string? videoP = result?.FullPath;
            if (videoP == null)
            {
                return;
            }
            videoPath = videoP;
            Entry vid = (Entry)FindByName("Source");
            vid.Text = videoPath;
            result = await PickAndShow(false);
            if (result == null)
            {
                return;
            }
            string? oPath = result?.FullPath;
            if (oPath == null)
            {
                return;
            }
            outputPath = oPath;
            Entry stitch = (Entry)FindByName("Stitch");
            stitch.Text = outputPath;
        }

        private async Task asyncStitchUp(string videoPath, string outputPath, int startTimeSeconds)
        {


            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, args) =>
            {

                if (BindingContext is MyViewModel viewModel)
                {
                    MainThread.InvokeOnMainThreadAsync(() => viewModel.IsBussy = true);
                }
                // Call the stitching process
                var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoPath, outputPath, startTimeSeconds);
                videoLength = videoStitcher.Stitch();
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                StoppActivity();

                // Display the stitched image
                if (File.Exists(outputPath))
                {
                    CounterBtn.Text = "Done";
                }
                else
                {
                    CounterBtn.Text = "Failed";
                }
            };

            worker.RunWorkerAsync();
        }

        public async Task<FileResult> PickAndShow(bool isvideo)
        {
            PickOptions options;
            if(isvideo)
            {
                options = new PickOptions
                {
                    PickerTitle = "Please select a video file",
                    FileTypes = FilePickerFileType.Videos
                };
            }
            else
            {
                options = new PickOptions
                {
                    PickerTitle = "Please select an image file",
                    FileTypes = FilePickerFileType.Images
                };
            }
            try
            {
                var result = await FilePicker.Default.PickAsync(options);
                if (result != null)
                {
                    if (/*result.FileName.EndsWith("jpg", StringComparison.OrdinalIgnoreCase) ||*/
                        result.FileName.EndsWith("png", StringComparison.OrdinalIgnoreCase))
                    {
                        //using var stream = await result.OpenReadAsync();
                        //var image = ImageSource.FromStream(() => stream);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                // The user canceled or something went wrong
            }

            return null;
        }


    }
}
