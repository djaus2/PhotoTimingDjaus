using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;

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
            FinishTime.Visibility = Visibility.Hidden;
            FinishTimeLabel.Visibility = FinishTime.Visibility;

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

        private bool _isDragging = false;

        private void StitchedImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start drawing the line only when the mouse is over the image
            _isDragging = true;
            StitchedImage.CaptureMouse();

            // Get the mouse position relative to the stitched image
            System.Windows.Point position = e.GetPosition(StitchedImage);

            // Set the line's starting and ending points relative to the image
            VerticalLine.X1 = position.X;
            VerticalLine.X2 = position.X;
            VerticalLine.Y1 = 0; // Top of the image
            VerticalLine.Y2 = StitchedImage.ActualHeight; // Bottom of the image

            // Make the line visible
            VerticalLine.Visibility = Visibility.Visible;
            TimeLabel.Visibility = Visibility.Visible;
            UpdateTimeLabel(position.X);
        }

        private void StitchedImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                // Update the line's position as the mouse moves over the stitched image
                System.Windows.Point position = e.GetPosition(StitchedImage);
                double posX = position.X;
                if (StitchedImage.LayoutTransform is ScaleTransform transform)
                {
                    double horizontalScale = transform.ScaleX; // Get the horizontal scale
                    posX = position.X * horizontalScale; // Adjust time based on scale
                }

                VerticalLine.X1 = posX;
                VerticalLine.X2 = posX;
                VerticalLine.Y1 = 0; // Top of the image
                double posY2 = StitchedImage.ActualHeight;
                if (StitchedImage.LayoutTransform is ScaleTransform transformV)
                {
                    double verticalScale = transformV.ScaleY; // Get the horizontal scale
                    posY2 = posY2 * verticalScale; // Adjust time based on scale
                }
                VerticalLine.Y2 = posY2; // Bottom of the image
                TimeLabel.Margin = new Thickness(posX + 10, 100, 0, 0); // Place label slightly to the right of the cursor

                UpdateTimeLabel(posX);
            }
        }

        private void StitchedImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Stop dragging and hide the line
            _isDragging = false;
            StitchedImage.ReleaseMouseCapture();
            VerticalLine.Visibility = Visibility.Collapsed;
            VerticalLine.Visibility = Visibility.Collapsed;
            TimeLabel.Visibility = Visibility.Collapsed;
            FinishTime.Visibility = Visibility.Visible;
            FinishTimeLabel.Visibility = FinishTime.Visibility;
        }

        private void UpdateTimeLabel(double positionX)
        {
            // Get the image's horizontal scaling from the LayoutTransform (ScaleTransform)
            double horizontalScale = 1.0; // Default scale (no zoom)
            if (StitchedImage.LayoutTransform is ScaleTransform transform)
            {
                horizontalScale = transform.ScaleX; // Get the horizontal scale
            }

            // Calculate the relative position accounting for the horizontal scale
            double relativePosition = (positionX / horizontalScale) / StitchedImage.ActualWidth;

            // Example total duration of the stitched video
            double durationInSeconds = videoLength; // Replace with the actual duration of your stitched image
            double timeInSeconds = (startTimeSeconds + relativePosition * durationInSeconds);
            TimeSpan ts = TimeSpan.FromMilliseconds((long)(timeInSeconds*1000));
            string formattedTime = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{(int)(ts.Milliseconds/10)}"; // Format as HH:MM:SS.hh
            // Display the calculated time
            TimeLabel.Text = $"{timeInSeconds:F2} sec";
            FinishTimeLabel.Text = formattedTime;
            FinishTime.Visibility = Visibility.Hidden;
            FinishTimeLabel.Visibility = FinishTime.Visibility;
            Clipboard.SetData(DataFormats.Text, (Object)formattedTime);
        }

    }
}