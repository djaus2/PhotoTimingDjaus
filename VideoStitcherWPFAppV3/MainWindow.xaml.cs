using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Controls;
using PhotoTimingDjaus;
using DetectAudioFlash;
using PhotoTimingDjaus.Enums;
using System.Diagnostics.Eventing.Reader;
using PhotoTimingGui.ViewModels;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows.Threading;
//using OpenCvSharp;


namespace PhotoTimingGui
{

    public partial class MainWindow : Window
    {
        private PhotoTimingDjaus.VideoStitcher? videoStitcher = null;
        private int margin = 20;
        private double videoLength = 0;
        private double selectedStartTime = 0; // Start time in seconds
        private string guninfoFilePath = @"C:\temp\vid\guninfo.txt";
        private int threshold = 1000; // Threshold for gun sound detection. Gun time is First time sound reaches max/threshold

        private double GunTimeDbl { get; set; }
        private int GunTimeIndex = 0; // Index of the gun time in the guninfo.txt file
        private OpenCvSharp.Scalar GunTimeColor = new OpenCvSharp.Scalar(255, 255, 255, 1); // OpenCV color for red (BGR format)

        //public Visibility MyVisibility { get; set; } = Visibility.Visible;
        private readonly DispatcherTimer _saveTimer;

        public MainWindow()
        {
            InitializeComponent();
            LoadViewModel();
            //this.DataContext = new ViewModels.MyViewModel();
            Loaded += MainWindow_Loaded;
            _saveTimer = new DispatcherTimer
            {
                //Save viewModel after 1 second of inactivity
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop();
                SaveViewModel();
            };

            if (this.DataContext is MyViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    _saveTimer.Stop();
                    _saveTimer.Start();
                };
            }
        }
        bool imageLoaded = false;
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            //OpenFileDialog openFileDialog = new OpenFileDialog
            //{
            //    Filter = "Image Files (*.png)|*.png"
            //};
            string imageFilePath = OutputPathInput.Text;
            if (File.Exists(imageFilePath))
            {
                // Load the selected image into the Image control
                BitmapImage bitmap = new BitmapImage(new Uri(imageFilePath));
                StitchedImage.Source = bitmap;

                // Save the original dimensions of the image
                ImageCanvas.Width = bitmap.PixelWidth;
                ImageCanvas.Height = bitmap.PixelHeight;

                imageLoaded = true;
            }
            else
            {
                MessageBox.Show("The specified image file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                imageLoaded = false;
                return;
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


        bool firstradioMessage = true;
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                if (radioButton.IsChecked.HasValue && radioButton.IsChecked.Value) // Fix for CS8629
                {
                    string? content = radioButton.Content.ToString();
                    if (!string.IsNullOrEmpty(content))
                    {
  
                        content = content.Trim().Replace(":", "");
                        string msg = "";
                        switch (content)
                        {
                            case "Button":
                                msg = "Timing is from video start.";
                                break;
                            case "Mic":
                                msg = "Timing is from start gun audio max/1000).";
                                break;
                            case "Flash":
                                msg = "Not yet implemented: Timing from visual flash (2Do). Using Default: Timing is from video start.";
                                break;
                        }
                        string Title = "Timing Mode";
                        if (firstradioMessage)
                        {
                            firstradioMessage = false;
                            Title = "Timing Mode (First)";
                            msg = $"You can change the timing mode by clicking on one of the radio buttons. DEFAULT {radioButton.Content}:   {msg}";
                            MessageBox.Show($"{msg}", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else 
                            MessageBox.Show($"You selected: {radioButton.Content}: {msg}", Title, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
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
            TimeFromMode timeFromMode = GetTimeFromMode();

            // Show the busy indicator
            BusyIndicator.Visibility = Visibility.Visible;
            //MyVisibility = Visibility.Collapsed; ; // Hide the button
            SetMyVisibility(Visibility.Collapsed);
            Thread.Yield();
            DetectVideoFlash.ActionVideoAnalysis? actionVideoAnalysis = null;
            VideoDetectMode videoDetectMode = GetVideoDetectMode();
            if (timeFromMode == TimeFromMode.FromButtonPress)
            {
                // Nothing 2Do
            }
            else if (timeFromMode == TimeFromMode.FromGunviaAudio)
            {
                //DetectVideoFlash.FFMpegActions.Filterdata(videoFilePath, guninfoFilePath);
            }
            else if(timeFromMode == TimeFromMode.FromGunViaVideo)
            {
            }
            else if (timeFromMode == TimeFromMode.ManuallySelect)
            {
            }
            // Validate inputs
            // Read inputs
            string gunAudioPath = GunAudioPathInput.Text;
            string outputPath = OutputPathInput.Text;


            StitchButton.Width = 0;
            StitchButton.IsEnabled = false; // Disable the button to prevent multiple clicks
                                            // Validate inputs
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath); // Delete existing output file if it exists
                //return;
            }

            if (!File.Exists(videoFilePath))
            {
                MessageBox.Show("The specified video file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StitchButton.IsEnabled = true;
                return;
            }

            if (!double.TryParse(StartTimeInput.Text, out selectedStartTime))
            {
                MessageBox.Show("Please enter a valid start time (seconds as decimal).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                StitchButton.IsEnabled = true;
                return;
            }

            if (!int.TryParse(Threshold.Text, out int _threshold))
            {
                MessageBox.Show("Please enter a valid number >0  (Typical 1000) for threshold.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                threshold = _threshold;
                return;
            }
            threshold = int.Parse(Threshold.Text);

            //if (videoStitcher == null)
            //{
                videoStitcher = new PhotoTimingDjaus.VideoStitcher(
                videoFilePath,
                (DataContext as ViewModels.MyViewModel)?.GunColor ?? new OpenCvSharp.Scalar(255, 255, 255, 1),
                outputPath,
                selectedStartTime,
                axisHeight,
                audioHeight,
                timeFromMode,
                threshold);
            //}


            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();
            
            


            worker.DoWork += (s, args) =>
            {
                //Determine guntime
                
                if (timeFromMode == TimeFromMode.FromButtonPress)
                {
                    GunTimeDbl = 0; // Default value when timing is from button press
                    GunTimeIndex = 0; // Default index when timing is from button press
                }
                else if (timeFromMode == TimeFromMode.FromGunviaAudio)
                {
                    GunTimeDbl = videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    GunTimeIndex = videoStitcher.GunTimeIndex;
                }
                else if (timeFromMode == TimeFromMode.FromGunViaVideo)
                {
                    GunTimeDbl = videoStitcher.GetGunTimenFrameIndex(gunAudioPath,videoDetectMode);
                    GunTimeIndex = videoStitcher.GunTimeIndex;
                }
                else if (timeFromMode == TimeFromMode.ManuallySelect)
                {
                    GunTimeDbl = videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    GunTimeIndex = videoStitcher.GunTimeIndex;
                }

                videoLength = videoStitcher.videoDuration; 
                videoStitcher.Stitch();              
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                GunTime.Text = $"{GunTimeDbl}";
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
                    /*if (timeFromMode == TimeFromMode.FromButtonPress)
                    {
                        GunTimeDbl = 0;
                    }
                    else if (timeFromMode == TimeFromMode.FromGunviaAudio)
                    {
                        GunTimeDbl = videoStitcher.GunTime;
                    }
                    else if (timeFromMode == TimeFromMode.FromGunViaVideo)
                    {
                        if (actionVideoAnalysis == null)
                        {
                            MessageBox.Show("ActionVideoAnalysis is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        GunTimeIndex = actionVideoAnalysis.GunTimeIndex;
                        GunTimeDbl = actionVideoAnalysis.GunTime;
                    }*/
                    
                    

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
                //MyVisibility = Visibility.Visible;
                SetMyVisibility(Visibility.Visible);
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
            if (tim < GunTimeDbl)
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
                if (tim< GunTimeDbl)
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
            double timeInSeconds = (selectedStartTime + relativePosition * durationInSeconds - GunTimeDbl);
            //System.Diagnostics.Debug.WriteLine($"{timeInSeconds} = {selectedStartTime} + {relativePosition}*{durationInSeconds}-{GunTimeDbl}");
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


        private void FlashButton_Click(object sender, RoutedEventArgs e)
        {
            VideoDetectMode vm = GetVideoDetectMode();

            double threshold = int.Parse(Threshold.Text); // Default threshold value, can be adjusted
            DetectVideoFlash.ActionVideoAnalysis actionVideoAnalysis 
                = new DetectVideoFlash.ActionVideoAnalysis(VideoPathInput.Text,vm, threshold);
            actionVideoAnalysis.ProcessVideo();
            //GunTime.Text = $"{actionVideoAnalysis.ProcessVideo()}";
        }

        /*
        private void ApplyColor_Click(object sender, RoutedEventArgs e)
        {
            string colorName = ColorTextBox.Text.Trim();

            try
            {
                SolidColorBrush clr = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorName));
            }
            catch
            {
                MessageBox.Show("Invalid color name. Try again.");
            }
        }

        private void ApplyOpenCVColor_Click(object sender, RoutedEventArgs e)
        {
            string colorName = ColorTextBox.Text.Trim().ToUpper();
            OpenCvSharp.Scalar selectedColor;

            switch (colorName)
            {
                case "RED":
                    selectedColor = new OpenCvSharp.Scalar(0, 0, 255, 1);
                    break;
                case "GREEN":
                    selectedColor = new OpenCvSharp.Scalar(0, 255, 0, 1);
                    break;
                case "BLUE":
                    selectedColor = new OpenCvSharp.Scalar(255, 0, 0, 1);
                    break;
                case "YELLOW":
                    selectedColor = new OpenCvSharp.Scalar(0, 255, 255, 1);
                    break;
                case "CYAN":
                    selectedColor = new OpenCvSharp.Scalar(255, 255, 0, 1);
                    break;
                case "MAGENTA":
                    selectedColor = new OpenCvSharp.Scalar(255, 0, 255, 1);
                    break;
                case "WHITE":
                    selectedColor = new OpenCvSharp.Scalar(255, 255, 255, 1);
                    break;
                case "BLACK":
                    selectedColor = new OpenCvSharp.Scalar(0, 0, 0, 1);
                    break;
                default:
                    MessageBox.Show("Invalid color name. Try again.");
                    return;
            }

            MessageBox.Show($"You selected OpenCV color: {colorName} ({selectedColor.Val0}, {selectedColor.Val1}, {selectedColor.Val2}, , {selectedColor.Val3})");
        }

        private void ColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                string colorName = menuItem.Header.ToString().ToUpper();
                OpenCvSharp.Scalar selectedColor;

                switch (colorName)
                {
                    case "RED":
                        selectedColor = new OpenCvSharp.Scalar(0, 0, 255, 1);
                        break;
                    case "GREEN":
                        selectedColor = new OpenCvSharp.Scalar(0, 255, 0, 1);
                        break;
                    case "BLUE":
                        selectedColor = new OpenCvSharp.Scalar(255, 0, 0, 1);
                        break;
                    case "YELLOW":
                        selectedColor = new OpenCvSharp.Scalar(0, 255, 255, 1);
                        break;
                    case "CYAN":
                        selectedColor = new OpenCvSharp.Scalar(255, 255, 0, 1);
                        break;
                    case "MAGENTA":
                        selectedColor = new OpenCvSharp.Scalar(255, 0, 255, 1);
                        break;
                    case "WHITE":
                        selectedColor = new OpenCvSharp.Scalar(255, 255, 255, 1);
                        break;
                    case "BLACK":
                        selectedColor = new OpenCvSharp.Scalar(0, 0, 0, 1);
                        break;
                    default:
                        MessageBox.Show("Invalid color selection.");
                        return;
                }
                GunTimeColor = selectedColor;
                MessageBox.Show($"You selected OpenCV color: {colorName} ({selectedColor.Val0}, {selectedColor.Val1}, {selectedColor.Val2}, {selectedColor.Val3})");
            }
        }
        */
        ////////////////////////////// File Menu ///////////////////////////////
        
        private void OpenMp4File_Click(object sender, RoutedEventArgs e)
        {
            string videoFilePath = VideoPathInput.Text;
            OpenFileDialog openFileDialog;
            if (File.Exists(videoFilePath)) 
            {
                string? initialDirectory = System.IO.Path.GetDirectoryName(videoFilePath);
                if (!string.IsNullOrEmpty(initialDirectory) && !Directory.Exists(initialDirectory))
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "MP4 Files (*.mp4)|*.mp4",
                        InitialDirectory = initialDirectory, // Set the folder
                        FileName = videoFilePath
                    };
                }
                else
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "MP4 Files (*.mp4)|*.mp4",

                    };
                }
            }
            else {
                openFileDialog = new OpenFileDialog
                {
                    Filter = "MP4 Files (*.mp4)|*.mp4",
                };
            }

            if (openFileDialog.ShowDialog() == true)
            {
                videoFilePath = openFileDialog.FileName;
                VideoPathInput.Text = videoFilePath;
            }
        }

        private void OpenPngFile_Click(object sender, RoutedEventArgs e)
        {
            string OutputFilePath = OutputPathInput.Text;
            OpenFileDialog openFileDialog;
            if (File.Exists(OutputFilePath))
            {
                string? initialDirectory = System.IO.Path.GetDirectoryName(OutputFilePath);
                if (!string.IsNullOrEmpty(initialDirectory) && !Directory.Exists(initialDirectory))
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "PNG Files (*.png)|*.png",
                        InitialDirectory = initialDirectory, // Set the folder
                        FileName = OutputFilePath
                    };
                }
                else
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "PNG Files (*.png)|*.png",
                    };
                }
            }
            else
            {
                openFileDialog = new OpenFileDialog
                {
                    Filter = "PNG Files (*.png)|*.png",
                };
            }

            if (openFileDialog.ShowDialog() == true)
            {
                OutputFilePath = openFileDialog.FileName;
                OutputPathInput.Text = OutputFilePath;
            }
        }

        private void OpenGunAudioTextFile_Click(object sender, RoutedEventArgs e)
        {
            string GunAudioPathInput = this.GunAudioPathInput.Text;
            OpenFileDialog openFileDialog;
            if (File.Exists(GunAudioPathInput))
            {
                string? initialDirectory = System.IO.Path.GetDirectoryName(GunAudioPathInput);
                if (!string.IsNullOrEmpty(initialDirectory) && !Directory.Exists(initialDirectory))
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "TXT Files (*.txt)|*.txt",
                        InitialDirectory = initialDirectory, // Set the folder
                        FileName = GunAudioPathInput
                    };
                }
                else
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "TXT Files (*.txt)|*.txt",
                    };
                }
            }
            else
            {
                openFileDialog = new OpenFileDialog
                {
                    Filter = "TXT Files (*.txt)|*.txt",
                };
            }

            if (openFileDialog.ShowDialog() == true)
            {
                GunAudioPathInput = openFileDialog.FileName;
                this.GunAudioPathInput.Text = GunAudioPathInput;
            }
        }

        /////////////////////////////////////////////////////////////////////////
        ///
        private void TimeFromMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TimeFromMode timeMode)
            {
                // Update the view model's TimeFromMode property.
                // Replace 'viewModel' with your actual view model reference or use DataContext.
                if (DataContext is ViewModels.MyViewModel viewModel)
                {
                    viewModel.TimeFromMode = timeMode;
                }
            }
        }

        private void VideoDetectMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is VideoDetectMode detectMode)
            {
                // Update the view model's VideoDetectMode property.
                if (DataContext is ViewModels.MyViewModel viewModel)
                {
                    viewModel.VideoDetectMode = detectMode;
                }
            }
        }

      

        public void SaveViewModel()
        {  
            MyViewModel viewModel = (this.DataContext as MyViewModel) ?? new MyViewModel(); // Ensure viewModel is not null, otherwise create a new instance
            string json = JsonSerializer.Serialize(viewModel);
            VideoStitcherWPFAppV3.Properties.Settings.Default.SavedViewModel = json;
            VideoStitcherWPFAppV3.Properties.Settings.Default.Save(); // Persist settings
        }

        public void LoadViewModel()
        {
            string json = VideoStitcherWPFAppV3.Properties.Settings.Default.SavedViewModel;
            if(!string.IsNullOrEmpty(json))
            {
                this.DataContext = JsonSerializer.Deserialize<MyViewModel>(json) ?? new MyViewModel();
            }
            else
            {
                this.DataContext = new MyViewModel();
            }
            if(this.DataContext is MyViewModel viewModel)
            {
                // Set default visibility at start to visble for controls
                //Add any other defaults here.
                viewModel.MyVisibility = Visibility.Visible; 
            }
        }


    }
}