using OpenCvSharp;
using PhotoTimingDjaus;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;



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
            StitchUp(videoPath, outputPath, startTimeSeconds);
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

        private void StitchUp(string videoPath, string outputPath, int startTimeSeconds)
        {


            if (!File.Exists(videoPath))
            {
                return;
            }

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
                    //Maybe add image later.
                    /*var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(outputPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    StitchedImage.LayoutTransform = null;
                    StitchedImage.Source = bitmap;
                    if (StitchedImage.Source is BitmapSource bitmapx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Image Dimensions: {bitmapx.PixelWidth}x{bitmap.PixelHeight}");
                    }
                    //Details.Visibility = Visibility.Collapsed;
                    //Scrolls.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Stitched image successfully created and displayed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    */
                }
                else
                {
                    CounterBtn.Text = "Failed";
                }
            };

            worker.RunWorkerAsync();
        }

        private void CounterBtn_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }


    }

}
