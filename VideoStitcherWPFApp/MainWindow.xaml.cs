using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Input;

namespace VideoStitcherGUI
{
    public partial class MainWindow : Window
    {
        int startTimeSeconds = 0;
        int videoLength = 0;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StitchButton_Click1(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read inputs
                string videoPath = VideoPathInput.Text;
                string outputPath = OutputPathInput.Text;
                int startTimeSeconds = int.Parse(StartTimeInput.Text);

                // Validate inputs
                if (!File.Exists(videoPath))
                {
                    MessageBox.Show("The specified video file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create instance of VideoStitcher and call Stitch()
                var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoPath, outputPath, startTimeSeconds);
                videoStitcher.Stitch();

                // Display the stitched image
                if (File.Exists(outputPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(outputPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    StitchedImage.Source = bitmap;
                    MessageBox.Show("Stitched image successfully created and displayed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to create the stitched image.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


            private void StitchButton_Click(object sender, RoutedEventArgs e)
            {
                // Validate inputs
                // Read inputs
                string videoPath = VideoPathInput.Text;
                string outputPath = OutputPathInput.Text;

                StitchButton.IsEnabled = false; // Disable the button to prevent multiple clicks
                                                // Validate inputs
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

        private Point _lastMousePosition;
        private bool _isPanning = false;

        /*
         * 
        private void ImageScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ImageScrollViewer.IsMouseOver)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(ImageScrollViewer);
                ImageScrollViewer.CaptureMouse();
            }
        }

        private void ImageScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                Point currentPosition = e.GetPosition(ImageScrollViewer);
                Vector delta = _lastMousePosition - currentPosition;

                ImageScrollViewer.ScrollToHorizontalOffset(ImageScrollViewer.HorizontalOffset + delta.X);
                ImageScrollViewer.ScrollToVerticalOffset(ImageScrollViewer.VerticalOffset + delta.Y);

                _lastMousePosition = currentPosition;
            }
        }

        private void ImageScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ImageScrollViewer.ReleaseMouseCapture();
        }*/
        

        //private bool _isDragging = false;

        /*
        private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Capture mouse and start drawing the line
            _isDragging = true;
            OverlayCanvas.CaptureMouse();

            // Get the starting position
            Point position = e.GetPosition(OverlayCanvas);

            // Set the line's starting and ending points
            VerticalLine.X1 = position.X;
            VerticalLine.X2 = position.X;
            VerticalLine.Y1 = 0; // Top of the canvas
            VerticalLine.Y2 = OverlayCanvas.ActualHeight; // Bottom of the canvas

            // Make the line visible
            VerticalLine.Visibility = Visibility.Visible;
        }

        private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                // Update the line's position as the mouse moves
                Point position = e.GetPosition(OverlayCanvas);
                VerticalLine.X1 = position.X;
                VerticalLine.X2 = position.X;
            }
        }

        private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Release the mouse and hide the line
            _isDragging = false;
            OverlayCanvas.ReleaseMouseCapture();
            VerticalLine.Visibility = Visibility.Collapsed;
        }*/

        private bool _isDragging = false;

        private void StitchedImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start drawing the line only when the mouse is over the image
            _isDragging = true;
            StitchedImage.CaptureMouse();
            
            // Get the mouse position relative to the stitched image
            Point position = e.GetPosition(StitchedImage);

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
                Point position = e.GetPosition(StitchedImage);
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
                TimeLabel.Margin = new Thickness(posX + 10, position.Y, 0, 0); // Place label slightly to the right of the cursor

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
        }

        private void StitchedImage_MouseLeftButtonUp2(object sender, MouseButtonEventArgs e)
        {
            // Stop dragging and hide the line and label
            _isDragging = false;
            StitchedImage.ReleaseMouseCapture();
            VerticalLine.Visibility = Visibility.Collapsed;
            TimeLabel.Visibility = Visibility.Collapsed;
        }

        private void UpdateTimeLabel1(double positionX)
        {
            // Calculate the time based on the X position
            double relativePosition = positionX / StitchedImage.ActualWidth;
            double durationInSeconds = videoLength; // Example total duration of the stitched video
            double timeInSeconds = startTimeSeconds +  relativePosition * durationInSeconds;

            // Display the calculated time
            TimeLabel.Text = $"{Math.Floor(timeInSeconds)} sec";
        }

        private void UpdateTimeLabel(double positionX)
        {
            // Get the image's horizontal scaling from the LayoutTransform (ScaleTransform)
            double horizontalScale = 1.0; // Default scale (no zoom)
            //if (StitchedImage.LayoutTransform is ScaleTransform transform)
            //{
            //    horizontalScale = transform.ScaleX; // Get the horizontal scale
            //}

            // Calculate the relative position accounting for the horizontal scale
            double relativePosition = (positionX / horizontalScale) / StitchedImage.ActualWidth;

            // Example total duration of the stitched video
            double durationInSeconds = videoLength; // Replace with the actual duration of your stitched image
            double timeInSeconds = (int)(startTimeSeconds +  relativePosition * durationInSeconds);

            // Display the calculated time
            TimeLabel.Text = $"{Math.Floor(timeInSeconds)} sec";
        }
    }
}