using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Controls;


namespace PhotoTimingGui
{

    using System.ComponentModel;
    using System.Windows;

  
    public partial class MainWindow : Window
    {

        private int margin = 20;
        private double videoLength = 0;
        private int startTimeSeconds = 0; // Start time in seconds
        private string guninfoFilePath = @"C:\temp\vid\guninfo.txt";
        private double gunTimeDbl { get; set; }

        public Visibility MyVisibility { get; set; } = Visibility.Visible;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        bool imageLoaded = false;
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png)|*.png"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Load the selected image into the Image control
                BitmapImage bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                StitchedImage.Source = bitmap;

                // Save the original dimensions of the image
                ImageCanvas.Width = bitmap.PixelWidth;
                ImageCanvas.Height = bitmap.PixelHeight;

                imageLoaded = true;
            }
        }

        private void HorizontalZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateZoom();
        }

        private void VerticalZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateZoom();
        }


        private void HorizontalPanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePan();
        }

        private void VerticalPanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePan();
        }

        private void UpdateCanvasBounds()
        {
            if (StitchedImage.Source is BitmapSource bitmap)
            {
                // Calculate scaled image dimensions
                double scaledWidth = bitmap.PixelWidth * HorizontalZoomSlider.Value;
                double scaledHeight = bitmap.PixelHeight * VerticalZoomSlider.Value;

                // Ensure the canvas accommodates either the scaled image or the viewer size (whichever is larger)
                ImageCanvas.Width = Math.Max(scaledWidth, ViewerBorder.ActualWidth);
                ImageCanvas.Height = Math.Max(scaledHeight, ViewerBorder.ActualHeight);

                // Constrain panning to the bounds of the visible viewer area
                double horizontalMaxPan = Math.Max(0, scaledWidth - ViewerBorder.ActualWidth);
                double verticalMaxPan = Math.Max(0, scaledHeight - ViewerBorder.ActualHeight);

                HorizontalPanSlider.Maximum = horizontalMaxPan;
                VerticalPanSlider.Maximum = verticalMaxPan;

                System.Diagnostics.Debug.WriteLine($"ViewerBorder Size: {ViewerBorder.ActualWidth}x{ViewerBorder.ActualHeight}");
                System.Diagnostics.Debug.WriteLine($"Canvas Size: {ImageCanvas.Width}x{ImageCanvas.Height}");
                System.Diagnostics.Debug.WriteLine($"Scaled Image Size: {bitmap.PixelWidth * HorizontalZoomSlider.Value}x{bitmap.PixelHeight * VerticalZoomSlider.Value}");
            }
        }

        private void AutoScaleCheckbox_Checked1(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                // Calculate scaling factor to fit the height of the Border
                double borderHeight = ViewerBorder.ActualHeight;
                //ImageCanvas.Height = ViewerBorder.ActualHeight;

                // Apply clipping region to the canvas
                //ImageCanvas.Clip = new RectangleGeometry(new Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));

                if (StitchedImage.Source is BitmapSource bitmap)
                {
                    // Scale the image proportionally to match the Border's height
                    double scaleFactor = borderHeight / bitmap.PixelHeight;

                    ScaleTransform scaleTransform = new ScaleTransform(1, scaleFactor);
                    StitchedImage.LayoutTransform = scaleTransform;
                    VerticalZoomSlider.IsEnabled = false;
                    VerticalPanSlider.IsEnabled = VerticalZoomSlider.IsEnabled;
                }
            }
        }

        private void AutoScaleCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                // Calculate the height of the Border, accounting for any border thickness
                double availableHeight = ViewerBorder.ActualHeight - ViewerBorder.BorderThickness.Top - ViewerBorder.BorderThickness.Bottom;

                if (StitchedImage.Source is BitmapSource bitmap)
                {
                    // Calculate scale factor to fit the height
                    double scaleFactor = availableHeight / bitmap.PixelHeight;

                    // Apply vertical scaling only
                    ScaleTransform scaleTransform = new ScaleTransform(1, scaleFactor);
                    StitchedImage.LayoutTransform = scaleTransform;

                    // Optionally center the image horizontally within the Canvas
                    double horizontalOffset = 0; // (ImageCanvas.Width - bitmap.PixelWidth * 1) / 2; // 1 = no horizontal scaling
                    Canvas.SetLeft(StitchedImage, horizontalOffset > 0 ? horizontalOffset : 0); // Ensure no negative offsets
                }
            }
        }

        private void AutoScaleCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (imageLoaded)
            {
                // Reset the image scaling to manual zoom levels
                VerticalZoomSlider.IsEnabled = true;
                VerticalPanSlider.IsEnabled = VerticalZoomSlider.IsEnabled;
                UpdateZoom();
            }
        }

        private void UpdatePan()
        {
            if (!imageLoaded)
                return;

            double horizontalOffset = HorizontalPanSlider.Value;
            double verticalOffset = VerticalPanSlider.Value;

            Canvas.SetLeft(StitchedImage, -horizontalOffset);
            Canvas.SetTop(StitchedImage, -verticalOffset);
        }

        private void UpdateZoom()
        {
            if (!imageLoaded)
                return;

            double horizontalScale = HorizontalZoomSlider.Value;
            double verticalScale = VerticalZoomSlider.Value;

            ScaleTransform scaleTransform = new ScaleTransform(horizontalScale, verticalScale);
            StitchedImage.LayoutTransform = scaleTransform;

            UpdateCanvasBounds(); // Update the panning sliders' max values
        }

        private void ViewerBorder_SizeChanged1(object sender, SizeChangedEventArgs e)
        {
            // Sync canvas size with the border size dynamically
            ImageCanvas.Width = ViewerBorder.ActualWidth;
            ImageCanvas.Height = ViewerBorder.ActualHeight;

            UpdateCanvasBounds(); // Recalculate pan limits
        }

        private void ViewerBorder_SizeChanged2(object sender, SizeChangedEventArgs e)
        {
            // Update canvas size to match the border dimensions
            ImageCanvas.Width = ViewerBorder.ActualWidth;
            ImageCanvas.Height = ViewerBorder.ActualHeight;

            // Apply clipping region to the canvas
            ImageCanvas.Clip = new RectangleGeometry(new Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));

            UpdateCanvasBounds(); // Recalculate pan and zoom limits
        }

        private void ViewerBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            return;
            // Handle dynamic resizing when auto-scaling is enabled
            if (AutoScaleCheckbox.IsChecked == true)
            {
                ImageCanvas.Width = ViewerBorder.ActualWidth;
                ImageCanvas.Height = ViewerBorder.ActualHeight;
                ImageCanvas.Clip = new RectangleGeometry(new Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));
                AutoScaleCheckbox_Checked(null, null);

            }
            else
            {
                ImageCanvas.Width = ViewerBorder.ActualWidth;
                ImageCanvas.Height = ViewerBorder.ActualHeight;

                // Apply clipping region to the canvas
                ImageCanvas.Clip = new RectangleGeometry(new Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));
                UpdateCanvasBounds();
            }
        }



        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateZoom();
        }

        private void StitchButton_Click(object sender, RoutedEventArgs e)
        {
            string videoFilePath = VideoPathInput.Text;
            int axisHeight = (int)AxisHeightSlider.Value;
            int audioHeight = (int)AudioHeightSlider.Value;

            // Show the busy indicator
            BusyIndicator.Visibility = Visibility.Visible;

            PhotoTimingDjausLib.FFMpegActions.Filterdata(videoFilePath, guninfoFilePath);

            // Validate inputs
            // Read inputs
            //string videoPath = VideoPathInput.Text;
            string outputPath = OutputPathInput.Text;

            StitchButton.Width = 0;
            StitchButton.IsEnabled = false; // Disable the button to prevent multiple clicks
                                            // Validate inputs
            MyVisibility = Visibility.Collapsed; ; // Hide the button
            if (!File.Exists(videoFilePath))
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

 
 

            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();
            var videoStitcher = new PhotoTimingDjaus.VideoStitcher(videoFilePath, outputPath, @"C:\temp\vid\guninfo.txt", startTimeSeconds, axisHeight, audioHeight);

            worker.DoWork += (s, args) =>
            {
                // Call the stitching process
                videoLength = videoStitcher.Stitch();
                
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                VideoLength.Text = $"{videoLength}";
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

                    gunTimeDbl = videoStitcher.GunTime;
                    GunTime.Text = $"{gunTimeDbl}";

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
                MyVisibility = Visibility.Visible;
                //StitchButton.Visibility = Visibility.Visible; // Hide the button
                StitchButton.Width = 200;
                StitchButton.IsEnabled = true; // Re-enable the button
            };

            worker.RunWorkerAsync();
        }

        private bool _isDragging = false;

        private void StitchedImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start drawing the line only when the mouse is over the image
            _isDragging = true;
            StitchedImage.CaptureMouse();

            // Get the mouse position relative to the stitched image
            System.Windows.Point position = e.GetPosition(StitchedImage);
            double posX = position.X;
            if (StitchedImage.LayoutTransform is ScaleTransform transform)
            {
                double horizontalScale = transform.ScaleX; // Get the horizontal scale
                posX = position.X * horizontalScale; // Adjust time based on scale
            }
            if (posX > StitchedImage.ActualWidth)
            {
                //if clicked after video ends hide line and text at mouse position
                TimeLabel.Visibility = Visibility.Collapsed;
                VerticalLine.Visibility = Visibility.Collapsed;
                return;
            }
            double tim = (posX / StitchedImage.ActualWidth) * videoLength;
            System.Diagnostics.Debug.WriteLine($"{tim} {gunTimeDbl}");
            if (tim < gunTimeDbl)
            {
                TimeLabel.Visibility = Visibility.Collapsed;
                VerticalLine.Visibility = Visibility.Collapsed;
                return;
            }
            TimeLabel.Visibility = Visibility.Visible; // Ensure the label is visible when clicked or dragged
            VerticalLine.Visibility = Visibility.Visible; // Ensure the line is visible when clicked or dragged

            // Set the line's starting and ending points relative to the image
            VerticalLine.X1 = position.X;
            VerticalLine.X2 = position.X;
            VerticalLine.Y1 = 0; // Top of the image
            VerticalLine.Y2 = StitchedImage.ActualHeight; // Bottom of the image

            // Make the line visible
            VerticalLine.Visibility = Visibility.Visible;

            // Make time label visible and position it
            TimeLabel.Visibility = Visibility.Visible;
            TimeLabel.TextAlignment = TextAlignment.Left; // Align text to the left
            TimeLabel.Margin = new Thickness(posX + 10, 100, 0, 0); // Place label slightly to the right of the cursor
            
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
                if (posX > StitchedImage.ActualWidth)
                {
                    //if clicked after video ends hide line and text at mouse position
                    TimeLabel.Visibility = Visibility.Collapsed;
                    VerticalLine.Visibility = Visibility.Collapsed;
                    return;
                }
                double tim = (posX / StitchedImage.ActualWidth) * videoLength;
                if (tim< gunTimeDbl)
                {
                    TimeLabel.Visibility = Visibility.Collapsed;
                    VerticalLine.Visibility = Visibility.Collapsed;
                    return;
                }
                TimeLabel.Visibility = Visibility.Visible; // Ensure the label is visible when clicked or dragge
                VerticalLine.Visibility = Visibility.Visible; // Ensure the line is visible when clicked or dragge
                
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
                TimeLabel.TextAlignment = TextAlignment.Left; // Align text to the left
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
            //FinishTime.Visibility = Visibility.Visible;
            //FinishTimeLabel.Visibility = FinishTime.Visibility;
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
            if(positionX> StitchedImage.ActualWidth)
            {
                TimeLabel.Text = $"";
                TimeLabel.Visibility = Visibility.Collapsed;
                return;
            }
            double relativePosition = (positionX / horizontalScale) / StitchedImage.ActualWidth;
            System.Diagnostics.Debug.WriteLine($"{positionX} {horizontalScale} {StitchedImage.ActualWidth}  {relativePosition}");
            // Example total duration of the stitched video
            double durationInSeconds = videoLength; // Replace with the actual duration of your stitched image
            VideoLength.Text = $"{videoLength} sec"; // Display the video length in seconds
            double timeInSeconds = (startTimeSeconds + relativePosition * durationInSeconds - gunTimeDbl);
            System.Diagnostics.Debug.WriteLine(timeInSeconds);
            if (timeInSeconds >= 0)
            {

                TimeSpan ts = TimeSpan.FromMilliseconds((long)(timeInSeconds * 1000));
                string formattedTime = $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{(int)(ts.Milliseconds / 10)}"; // Format as HH:MM:SS.hh
                                                                                                                       // Display the calculated time
                TimeLabel.Visibility = Visibility.Visible;
                TimeLabel.Text = $"{timeInSeconds:F2} sec";
                //FinishTime.Text = $"{timeInSeconds:F2} sec";
                FinishTime.Text = formattedTime;
                Clipboard.SetData(DataFormats.Text, (Object)formattedTime);
            }
            else
            {
                //FinishTime.Text = "";
                FinishTime.Text = $"{timeInSeconds:F2} sec";
                TimeLabel.Text = $"{timeInSeconds:F2} sec";
                TimeLabel.Visibility = Visibility.Collapsed;
                Clipboard.SetData(DataFormats.Text, (Object)"");
            }
                //FinishTime.Visibility = Visibility.Hidden;
                //FinishTimeLabel.Visibility = FinishTime.Visibility;*/
                //Clipboard.SetData(DataFormats.Text, (Object)formattedTime);
            //System.Diagnostics.Debug.WriteLine($"positionX:{positionX} horizontalScale:{horizontalScale} StitchedImage.ActualWidth:{StitchedImage.ActualWidth} relativePosition:{relativePosition} durationInSeconds:{durationInSeconds} {timeInSeconds} {ts} {formattedTime} {FinishTimeLabel.Text} {FinishTimeLabel.Text} ");
        }

        private void StitchedImage_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }
    }
}