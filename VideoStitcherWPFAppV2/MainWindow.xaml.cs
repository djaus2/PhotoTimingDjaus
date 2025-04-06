using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;

namespace VideoStitcherGUI
{
    public partial class MainWindow : Window
    {
        private int videoLength = 0;
        private int startTimeSeconds = 0; // Start time in seconds
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StitchButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            // Read inputs
            string videoPath = VideoPathInput.Text;
            string outputPath = OutputPathInput.Text;

            StitchButton.Width = 0;
            StitchButton.IsEnabled = false; // Disable the button to prevent multiple clicks
                                            // Validate inputs
            StitchButton.Visibility = Visibility.Hidden; // Hide the button
            if (!File.Exists(videoPath))
            {
                MessageBox.Show("The specified video file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StitchButton.IsEnabled = true;
                return;
            }

            if (!int.TryParse(StartTimeInput.Text, out startTimeSeconds))
            {
                MessageBox.Show("Please enter a valid number for Start Time (Seconds).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StitchButton.IsEnabled = true;
                return;
            }

            // Show the busy indicator
            BusyIndicator.Visibility = Visibility.Visible;

            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, args) =>
            {
                // Call the stitching process
                var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoPath, outputPath, startTimeSeconds);
                videoLength = videoStitcher.Stitch();
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                // Hide the busy indicator
                BusyIndicator.Visibility = Visibility.Collapsed;

                // Display the stitched image
                if (File.Exists(outputPath))
                {
                    var bitmap = new BitmapImage();
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
                }
                else
                {
                    MessageBox.Show("Failed to create the stitched image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                StitchButton.Visibility = Visibility.Visible; // Hide the button
                StitchButton.Width = 200;
                StitchButton.IsEnabled = true; // Re-enable the button
            };

            worker.RunWorkerAsync();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ensure the StitchedImage is not null and has an image source
            if (StitchedImage != null && StitchedImage.Source != null)
            {
                double zoomScale = e.NewValue;
                StitchedImage.LayoutTransform = new ScaleTransform(zoomScale, zoomScale);
            }
        }

        private void HorizontalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ensure the StitchedImage is not null and has an image source
            if (StitchedImage != null && StitchedImage.Source != null)
            {
                double horizontalScale = e.NewValue;
                double verticalScale = VerticalSlider.Value; // Maintain current vertical scale
                StitchedImage.LayoutTransform = new ScaleTransform(horizontalScale, verticalScale);
            }
        }

        private void VerticalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Ensure the StitchedImage is not null and has an image source
            if (StitchedImage != null && StitchedImage.Source != null)
            {
                double verticalScale = e.NewValue;
                double horizontalScale = HorizontalSlider.Value; // Maintain current horizontal scale
                StitchedImage.LayoutTransform = new ScaleTransform(horizontalScale, verticalScale);
            }
        }
    }
}