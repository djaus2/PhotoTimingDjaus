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
using System.Drawing;
using System.Windows.Controls.Primitives;
using OpenCvSharp.Features2D;
using static System.Net.Mime.MediaTypeNames;
using SharpVectors.Converters;
using System.Runtime.Intrinsics.Arm;
using System.Diagnostics;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using OpenCvSharp;
using System.Media;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Security;
using AthStitcher.ViewModels;
using PhotoTimingDjausLib;
//using OpenCvSharp;
//using OpenCvSharp;


namespace PhotoTimingGui
{

    public partial class MainWindow : System.Windows.Window
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
        private AthStitcherViewModel athStitcherViewModel;
        AthStitcherViewModel viewModel { get; set; } = new AthStitcherViewModel();

        public MainWindow()
        {
            InitializeComponent();
            athStitcherViewModel = new AthStitcherViewModel();
            this.DataContext = viewModel;
            athStitcherViewModel.LoadViewModel();
            this.DataContext = athStitcherViewModel.DataContext; // Set the DataContext to the AthStitchView instance
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
                athStitcherViewModel.SaveViewModel();
            };

            if (this.DataContext is AthStitcherModel vm)
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
            VerticalLine.Visibility = Visibility.Collapsed; // Hide the vertical line
            StartVerticalLine.Visibility = Visibility.Collapsed; // Hide the start vertical line
            NudgeVerticalLine.Visibility = Visibility.Collapsed;
            //OpenFileDialog openFileDialog = new OpenFileDialog
            //{
            //    Filter = "Image Files (*.png)|*.png"
            //};
            string imageFilePath = athStitcherViewModel.GetOutputPath();
            if (File.Exists(imageFilePath))
            {
                // Load the selected image into the Image control
                BitmapImage bitmap = new BitmapImage(new Uri(imageFilePath));
                StitchedImage.Source = bitmap;

                // Save the original dimensions of the image
                ImageCanvas.Width = bitmap.PixelWidth;
                ImageCanvas.Height = bitmap.PixelHeight;
                ImageCanvas.HorizontalAlignment = HorizontalAlignment.Left;
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
            if (VerticalLine != null)
            { VerticalLine.Visibility = Visibility.Collapsed; }
            if (StartVerticalLine != null)
            { StartVerticalLine.Visibility = Visibility.Collapsed; }
            if (NudgeVerticalLine != null)
            { NudgeVerticalLine.Visibility = Visibility.Collapsed; }
            if(TimeLabel != null)
                { TimeLabel.Visibility = Visibility.Collapsed; }
            UpdateZoom();
        }

        private void VerticalZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VerticalLine != null)
            { VerticalLine.Visibility = Visibility.Collapsed; }
            if (StartVerticalLine != null)
            { StartVerticalLine.Visibility = Visibility.Collapsed; }
            if (NudgeVerticalLine != null)
            { NudgeVerticalLine.Visibility = Visibility.Collapsed; }
            if (TimeLabel != null)
            { TimeLabel.Visibility = Visibility.Collapsed; }
            if (NudgePopupVideoFrameImage != null)
            { NudgePopupVideoFrameImage.Visibility = Visibility.Collapsed; }
            if (PopupVideoFrameImage != null)
            { PopupVideoFrameImage.IsOpen = false; }
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

        private void AutoScaleWidthCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (VerticalLine != null)
            { VerticalLine.Visibility = Visibility.Collapsed; }
            if (StartVerticalLine != null)
            { StartVerticalLine.Visibility = Visibility.Collapsed; }
            if (NudgeVerticalLine != null)
            { NudgeVerticalLine.Visibility = Visibility.Collapsed; }
            if (TimeLabel != null)
            { TimeLabel.Visibility = Visibility.Collapsed; }
            if (NudgePopupVideoFrameImage != null)
            { NudgePopupVideoFrameImage.Visibility = Visibility.Collapsed; }
            if (PopupVideoFrameImage != null)
            { PopupVideoFrameImage.IsOpen = false; }

            if (!(imageLoaded))
            {
                if (StitchedImage == null)
                    return;
            }
            if (HorizontalZoomSlider == null)
                return;
            if (VerticalZoomSlider == null)
                return;
            // Calculate the height of the Border, accounting for any border thickness
            double availableWidth = ViewerBorder.ActualWidth - ViewerBorder.BorderThickness.Left - ViewerBorder.BorderThickness.Right;

            if (StitchedImage.Source is BitmapSource bitmap)
            {
                // Calculate scale factor to fit the height
                double scaleFactor = availableWidth / bitmap.PixelWidth;
                // Ensure the scale factor is not less than 0.1 to avoid too small scaling
                if (scaleFactor < HorizontalZoomSlider.Minimum)
                    scaleFactor = VerticalZoomSlider.Minimum;
                if (scaleFactor > HorizontalZoomSlider.Maximum)
                    scaleFactor = HorizontalZoomSlider.Maximum;
                HorizontalZoomSlider.Value = scaleFactor;
                return;
                // Apply vertical scaling only
                ScaleTransform scaleTransform = new ScaleTransform(1, scaleFactor);
                StitchedImage.LayoutTransform = scaleTransform;

                // Optionally center the image horizontally within the Canvas
                //double horizontalOffset = 0; // (ImageCanvas.Width - bitmap.PixelWidth * 1) / 2; // 1 = no horizontal scaling
                //Canvas.SetLeft(StitchedImage, horizontalOffset > 0 ? horizontalOffset : 0); // Ensure no negative offsets
            }

        }

        private void AutoScaleWidthCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (VerticalLine != null)
            { VerticalLine.Visibility = Visibility.Collapsed; }
            if (StartVerticalLine != null)
            { StartVerticalLine.Visibility = Visibility.Collapsed; }
            if (NudgeVerticalLine != null)
            { NudgeVerticalLine.Visibility = Visibility.Collapsed; }
            if (TimeLabel != null)
            { TimeLabel.Visibility = Visibility.Collapsed; }
            if (NudgePopupVideoFrameImage != null)
            { NudgePopupVideoFrameImage.Visibility = Visibility.Collapsed; }
            if (PopupVideoFrameImage != null)
            { PopupVideoFrameImage.IsOpen = false; }

            if (!(imageLoaded))
            {
                if (StitchedImage == null)
                    return;
            }
            if (HorizontalZoomSlider == null)
                return;
            if (VerticalZoomSlider == null)
                return;
            // Reset the image scaling to manual zoom levels
            HorizontalZoomSlider.IsEnabled = true;
            HorizontalZoomSlider.IsEnabled = HorizontalZoomSlider.IsEnabled;
            UpdateZoom();
        }


        private void AutoScaleHeightCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (VerticalLine != null)
            { VerticalLine.Visibility = Visibility.Collapsed; }
            if (StartVerticalLine != null)
            { StartVerticalLine.Visibility = Visibility.Collapsed; }
            if (!(imageLoaded))
            {
                if (StitchedImage == null)
                    return;
            }
            if (HorizontalZoomSlider == null)
                return;
            if (VerticalZoomSlider == null)
                return;
            // Calculate the height of the Border, accounting for any border thickness
            double availableHeight = ViewerBorder.ActualHeight - ViewerBorder.BorderThickness.Top - ViewerBorder.BorderThickness.Bottom;

            if (StitchedImage.Source is BitmapSource bitmap)
            {
                // Calculate scale factor to fit the height
                double scaleFactor = availableHeight / bitmap.PixelHeight;
                // Ensure the scale factor is not less than 0.1 to avoid too small scaling
                if (scaleFactor < VerticalZoomSlider.Minimum)
                    scaleFactor = VerticalZoomSlider.Minimum;
                if (scaleFactor > VerticalZoomSlider.Maximum)
                    scaleFactor = VerticalZoomSlider.Maximum;
                VerticalZoomSlider.Value = scaleFactor;
                return;
                // Apply vertical scaling only
                ScaleTransform scaleTransform = new ScaleTransform(1, scaleFactor);
                StitchedImage.LayoutTransform = scaleTransform;

                // Optionally center the image horizontally within the Canvas
                //double horizontalOffset = 0; // (ImageCanvas.Width - bitmap.PixelWidth * 1) / 2; // 1 = no horizontal scaling
                //Canvas.SetLeft(StitchedImage, horizontalOffset > 0 ? horizontalOffset : 0); // Ensure no negative offsets
            }
            
        }

        private void AutoScaleHeightCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!(imageLoaded))
            {
                if (StitchedImage == null)
                    return;
            }
            if (HorizontalZoomSlider == null)
                return;
            if (VerticalZoomSlider == null)
                return;
            // Reset the image scaling to manual zoom levels
            VerticalZoomSlider.IsEnabled = true;
                VerticalPanSlider.IsEnabled = VerticalZoomSlider.IsEnabled;
                UpdateZoom();
        }

        private void UpdatePan()
        {
            if (!(imageLoaded))
            {
                if (StitchedImage == null)
                    return;
            }
            if (HorizontalZoomSlider == null)
                return;
            if (VerticalZoomSlider == null)
                return;

            double horizontalOffset = HorizontalPanSlider.Value;
            double verticalOffset = VerticalPanSlider.Value;

            Canvas.SetLeft(StitchedImage, -horizontalOffset);
            Canvas.SetTop(StitchedImage, -verticalOffset);
        }

        private void UpdateZoom()
        {
            if (!(imageLoaded))
            { 
                if (StitchedImage == null)
                    return;
            }
            if (HorizontalZoomSlider == null)
                return;
            if (VerticalZoomSlider == null)
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
            ImageCanvas.Clip = new RectangleGeometry(new System.Windows.Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));

            UpdateCanvasBounds(); // Recalculate pan and zoom limits
        }

        private void ViewerBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged)
            {
                ////return;
                // Handle dynamic resizing when auto-scaling is enabled
                if (AutoScaleHeightCheckbox.IsChecked == true)
                {
                    ImageCanvas.Width = ViewerBorder.ActualWidth;
                    ImageCanvas.Height = ViewerBorder.ActualHeight;
                    ImageCanvas.Clip = new RectangleGeometry(new System.Windows.Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));
                    AutoScaleHeightCheckbox_Checked(null, null);

                }
                else
                {
                    ImageCanvas.Width = ViewerBorder.ActualWidth;
                    ImageCanvas.Height = ViewerBorder.ActualHeight;

                    // Apply clipping region to the canvas
                    ImageCanvas.Clip = new RectangleGeometry(new System.Windows.Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));
                    UpdateCanvasBounds();
                }
            }
            else if (e.WidthChanged)
            {
                ////return;
                // Handle dynamic resizing when auto-scaling is enabled
                if (AutoScaleWidthCheckbox.IsChecked == true)
                {
                    ImageCanvas.Width = ViewerBorder.ActualWidth;
                    ImageCanvas.Height = ViewerBorder.ActualHeight;
                    ImageCanvas.Clip = new RectangleGeometry(new System.Windows.Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));
                    AutoScaleWidthCheckbox_Checked(null, null);

                }
                else
                {
                    ImageCanvas.Width = ViewerBorder.ActualWidth;
                    ImageCanvas.Height = ViewerBorder.ActualHeight;

                    // Apply clipping region to the canvas
                    ImageCanvas.Clip = new RectangleGeometry(new System.Windows.Rect(0, 0, ViewerBorder.ActualWidth, ViewerBorder.ActualHeight));
                    UpdateCanvasBounds();
                }
            }
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

        private  void StitchButton_Click(object sender, RoutedEventArgs e)
        {
            PopupVideoFrameImage.IsOpen = false; // Close the popup if it is open
            WatchClockDateTimePopup.IsOpen = false;
            StartVerticalLine.Visibility = Visibility.Collapsed;
            VerticalLine.Visibility = Visibility.Collapsed;
            TimeLabel.Visibility = Visibility.Collapsed;

            string videoFilePath = athStitcherViewModel.GetVideoPath();
            int axisHeight = (int)AxisHeightSlider.Value;
            int audioHeight = (int)AudioHeightSlider.Value;
            TimeFromMode timeFromMode = athStitcherViewModel.GetTimeFromMode();
            // Used by manully select mode, later
            // Need to get stitched image first
            // You can then set it.
            athStitcherViewModel.SetSelectedStartTime(0);
            VerticalLine.Visibility = Visibility.Collapsed; // Hide the vertical line
            StartVerticalLine.Visibility = Visibility.Collapsed; // Hide the start vertical line

            // Show the busy indicator
            BusyIndicator.Visibility = Visibility.Visible;
            //MyVisibility = Visibility.Collapsed; ; // Hide the button
            athStitcherViewModel.SetMyVisibility(Visibility.Collapsed);
            Thread.Yield();
            DetectVideoFlash.ActionVideoAnalysis? actionVideoAnalysis = null;
            VideoDetectMode videoDetectMode = athStitcherViewModel.GetVideoDetectMode();
            if (timeFromMode == TimeFromMode.FromButtonPress)
            {
                // Nothing 2Do
            }
            else if (timeFromMode == TimeFromMode.FromGunviaAudio)
            {
                //DetectVideoFlash.FFMpegActions.Filterdata(videoFilePath, guninfoFilePath);
            }
            else if (timeFromMode == TimeFromMode.FromGunViaVideo)
            {
            }
            else if (timeFromMode == TimeFromMode.ManuallySelect)
            {
            }
            // Validate inputs
            // Read inputs
            //string gunAudioPath = GunAudioPath();
            string outputPath = athStitcherViewModel.GetOutputPath();
            while (outputPath.Contains("_start_",StringComparison.OrdinalIgnoreCase))
            {
                outputPath = outputPath.Substring(0, outputPath.IndexOf("_Start_", StringComparison.OrdinalIgnoreCase));
                outputPath = $"{outputPath}.png";
            }
            athStitcherViewModel.SetOutputPath(outputPath);
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



            if (!int.TryParse(Threshold.Text, out int _threshold))
            {
                MessageBox.Show("Please enter a valid number >0  (Typical 1000) for threshold.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                threshold = _threshold;

                return;
            }
            threshold = int.Parse(Threshold.Text);

            DateTime? creationDate = DetectAudioFlash.FFMpegActions.GetVideoStart(athStitcherViewModel.GetVideoPath());
            if (creationDate != null)
            {
                athStitcherViewModel.SetVideoCreationDate(creationDate);
            }

            //if (videoStitcher == null)
            //{
            videoStitcher = new PhotoTimingDjaus.VideoStitcher(
                athStitcherViewModel.GetVideoPath(),
                athStitcherViewModel.GetGunColor(),
                athStitcherViewModel.GetOutputPath(),
                athStitcherViewModel.GetSelectedStartTime(),
                axisHeight,
                audioHeight,
                athStitcherViewModel.GetlevelImage(),
                athStitcherViewModel.GetTimeFromMode(),
                threshold);
            //}

            string gunAudioPath = athStitcherViewModel.GetGunAudioPath();
            videoDetectMode = athStitcherViewModel.GetVideoDetectMode();

            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();




            worker.DoWork += (s, args) =>
            {
                //Determine guntime

                if (timeFromMode == TimeFromMode.FromButtonPress)
                {
                    //Need next to get video length
                    var xx = videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
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
                    GunTimeDbl = videoStitcher.GetGunTimenFrameIndex(gunAudioPath, videoDetectMode);
                    GunTimeIndex = videoStitcher.GunTimeIndex;
                }
                else if (timeFromMode == TimeFromMode.ManuallySelect)
                {
                    //Need next to get video length
                    var xx = videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    GunTimeDbl = 0;
                    GunTimeIndex = 0;// videoStitcher.GunTimeIndex;
                }
                else if (timeFromMode == TimeFromMode.WallClockSelect)
                {
                    //Need next to get video length
                    //GunTimeDbl = 0.7;// videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    //GunTimeDbl = videoStitcher.GetGunTimenFrameIndex($"{ GunTimeDbl}");
                    //GunTimeIndex = videoStitcher.GunTimeIndex;
                }


                videoStitcher.Stitch();
                // Add metadata to the stitched image

            };

            worker.RunWorkerCompleted += async (s, args) =>
            {

                string imagepath =  PngMetadataHelper.AppendGunTimeImageFilename(athStitcherViewModel.GetOutputPath(), GunTimeDbl);
                if(!string.IsNullOrEmpty(imagepath))
                {
                    athStitcherViewModel.SetOutputPath(imagepath);
                }
                else
                {
                    MessageBox.Show("Failed to append gun time to image filename.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StitchButton.IsEnabled = true; // Re-enable the button
                    return;
                }

                videoLength = videoStitcher.videoDuration;
                athStitcherViewModel.SetVideoLength(videoLength);
                athStitcherViewModel.SetGunTime(GunTimeDbl, GunTimeIndex); // Set the gun time in the ViewModel

                athStitcherViewModel.SetVideoLength(videoLength);



                

                    // Hide the busy indicator
                    BusyIndicator.Visibility = Visibility.Collapsed;

                // Display the stitched image
                LoadStitchedImage(athStitcherViewModel.GetOutputPath());

                athStitcherViewModel.SetMyVisibility(Visibility.Visible);

                StitchButton.Width = 200;
                StitchButton.IsEnabled = true; // Re-enable the button
                athStitcherViewModel.SetHasStitched();
                if (timeFromMode == TimeFromMode.WallClockSelect)
                {
                    //If the wall clock start time is not set,
                    //.. thatis its is DateTime.MinValue
                    // set it to the video creation date
                    if (athStitcherViewModel.GetEventWallClockStartTime() == DateTime.MinValue)
                    {
                        athStitcherViewModel.SetEventWallClockStartTime(athStitcherViewModel.GetVideoCreationDate());
                    }
                    
                }
            };

            worker.RunWorkerAsync();
        }

        private bool _isDragging = false;

        private bool SetVerticalLine(System.Windows.Point position, System.Windows.Shapes.Line _VerticalLine)
        {
            PopupVideoFrameImage.IsOpen = false;
            if (_VerticalLine != null)
            { _VerticalLine.Visibility = Visibility.Collapsed; }
            TimeLabel.Visibility = Visibility.Collapsed;


            // Get the mouse position relative to the stitched image
            //System.Windows.Point position = e.GetPosition(StitchedImage);

            double posX = position.X;
            double horizontalScale = 1;
            double verticalScale = 1;
            if (StitchedImage.LayoutTransform is ScaleTransform transform)
            {
                horizontalScale = transform.ScaleX; // Get the horizontal scale
                verticalScale = transform.ScaleY;
                //posX = position.X * horizontalScale; // Adjust time based on scale
            }

            double stitchedImageVirtualWidth = StitchedImage.ActualWidth * horizontalScale;
            double stitchedImageVirtualHeight = StitchedImage.ActualHeight * verticalScale;
            if (posX > stitchedImageVirtualWidth)
            {
                _VerticalLine.Visibility = Visibility.Collapsed;
                return false;
                //if clicked after video ends hide line and text at mouse position
            }

            videoLength = athStitcherViewModel.GetVideoLength();
            double timeFromVideoStart = (posX / stitchedImageVirtualWidth) * videoLength;
            System.Diagnostics.Debug.WriteLine($"DOWN -- tim {timeFromVideoStart} = (posX {posX}/ sivw {stitchedImageVirtualWidth})* videoLength {videoLength}");
            double gunTime = athStitcherViewModel.GetGunTime();
            if (timeFromVideoStart < gunTime)
            {
                //Before gun so hide line
                _VerticalLine.Visibility = Visibility.Collapsed;
                return false;
            }

            // Set the line's starting and ending points relative to the image
            _VerticalLine.X1 = position.X;
            _VerticalLine.X2 = position.X;
            _VerticalLine.Y1 = 0; // Top of the image
            _VerticalLine.Y2 = stitchedImageVirtualHeight; // Bottom of the image

            // Make the line visible
            _VerticalLine.Visibility = Visibility.Visible;

            bool isLeft = (_VerticalLine == VerticalLine);
            UpdateTimeLabel(position.X, timeFromVideoStart, isLeft);

            return true;
        }

        private void StitchedImage_MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            NudgeVerticalLine.Visibility = Visibility.Collapsed;
            if (!athStitcherViewModel.IsDataContext())
                return;
            horizOffset = 0;
            TimeLabel.Visibility = Visibility.Collapsed;

            System.Windows.Shapes.Line _VerticalLine = VerticalLine;

            bool isLeft = (e.LeftButton == MouseButtonState.Pressed);
            if (isLeft)
            {
                if (!athStitcherViewModel.Get_HasStitched())
                    return;
                // Left button for Manuual Mode only if guntime has been set
                if (!athStitcherViewModel.Get_HaveSelectedandShownGunLineinManualorWallClockMode())
                    return;
                _VerticalLine = VerticalLine;
            }
            else
            {
                if (!(e.RightButton == MouseButtonState.Pressed))
                    return;
                if (!athStitcherViewModel.Get_HasStitched())
                    return;
                if (!athStitcherViewModel.ManuallySelectMode())
                    return;

                if (athStitcherViewModel.HasSelectedandShownGunLineToManualMode())
                    return;

                _VerticalLine = StartVerticalLine;
            }
            
            // Start drawing the line only when the mouse is over the image

            StitchedImage.CaptureMouse();

            // Set the vertical line position based on the mouse click position
            System.Windows.Point position = e.GetPosition(ImageCanvas);
            bool result = this.SetVerticalLine(position, _VerticalLine);
            if (!result)
            {
                // If the line was not set, exit the method
                return;
            }
            _isDragging = true;
        }

        private void StitchedImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!athStitcherViewModel.IsDataContext())
                return;
            //Jeteson quickly
            if ((!(e.LeftButton == MouseButtonState.Pressed)) && !(e.RightButton == MouseButtonState.Pressed))
                return;

            if (_isDragging)
            {
                System.Windows.Shapes.Line _VerticalLine = VerticalLine;
                bool isLeft = (e.LeftButton == MouseButtonState.Pressed);
                if (isLeft)
                {
                    _VerticalLine = VerticalLine;
                }
                else
                {
                    if (!(e.RightButton == MouseButtonState.Pressed))
                        return;
                    _VerticalLine = StartVerticalLine;
                }

                // Update the line's position as the mouse moves over the stitched image
                //System.Windows.Point position = e.GetPosition(StitchedImage);
                // Set the vertical line position based on the mouse click position
                System.Windows.Point position = e.GetPosition(ImageCanvas);
                bool result = this.SetVerticalLine(position, _VerticalLine);
                if (!result)
                {
                    // If the line was not set, exit the method
                    return;
                }
            }
        }
        int frameNo = 0;
        double Fps = 30;
        private void StitchedImage_MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;

            if ((VerticalLine.Visibility == Visibility.Visible) ||
                    (StartVerticalLine.Visibility == Visibility.Visible))
            {
                System.Windows.Point position = e.GetPosition(ImageCanvas);

                double posX = position.X;

                if (athStitcherViewModel.GetShowVideoFramePopup())
                    DisplayFrame(frameNo, posX);
            }
            StitchedImage.ReleaseMouseCapture();
        }


        private void UpdateTimeLabel(double positionX, double timeFromVideoStart, bool isLeftButton = true)
        {
            if (!athStitcherViewModel.IsDataContext())
                return;

            var margin = athStitcherViewModel.GetTimeLabelMargin();
            margin.Left += positionX;
            TimeLabel.Margin = margin;

            // Get the image's horizontal scaling from the LayoutTransform (ScaleTransform)
            double horizontalScale = 1;
            double verticalScale = 1;
            if (StitchedImage.LayoutTransform is ScaleTransform transform)
            {
                horizontalScale = transform.ScaleX; // Get the horizontal scale
                verticalScale = transform.ScaleY;
                //posX = position.X * horizontalScale; // Adjust time based on scale
            }
            double stitchedImageVirtualWidth = StitchedImage.ActualWidth * horizontalScale;
            double stitchedImageVirtualHeight = StitchedImage.ActualHeight * verticalScale;
            if (positionX > stitchedImageVirtualWidth)
            {
                TimeLabel.Text = $"";
                TimeLabel.Visibility = Visibility.Collapsed;
                return;
            }
            // Example total duration of the stitched video
            double durationInSeconds = athStitcherViewModel.GetVideoLength(); // Replace with the actual duration of your stitched image

            // Set default visibility at start to visible for controls
            //Add any other defaults here.

            double gunTime = 0;
            //With WallClock and Manual need to select gun time first
            if ((athStitcherViewModel.GetTimeFromMode() != TimeFromMode.WallClockSelect) &&
                    (athStitcherViewModel.GetTimeFromMode() != TimeFromMode.ManuallySelect))
            {
                // Get the gun time from the ViewModel
                gunTime = athStitcherViewModel.GetGunTime(); // Get the gun time from the ViewModel
            }
            else if (athStitcherViewModel.Get_HaveSelectedandShownGunLineinManualorWallClockMode())
            { 
                gunTime = athStitcherViewModel.GetGunTime(); // Get the gun time from the ViewModel
            }
            double timeFromGunStart = timeFromVideoStart - gunTime; // Calculate time from gun start
            frameNo = (int)(timeFromVideoStart * Fps); // Assuming 30 FPS, adjust as needed
            
            if (timeFromGunStart >= 0)
            {

                string formattedTime = $"{timeFromGunStart}";                                                                                                      // Display the calculated time
                TimeLabel.Visibility = Visibility.Visible;
                TimeLabel.Text = $"{timeFromGunStart:F2} sec";
                TimeLabel.TextAlignment = TextAlignment.Left; // Align text to the left

                //FinishTime.Text = $"{timeInSeconds:F2} sec";
                if (isLeftButton)
                {
                    FinishTime.Text = formattedTime;
                    Clipboard.SetData(DataFormats.Text, (Object)formattedTime);
                }
                else
                {

                    // Set default visibility at start to visble for controls
                    //Add any other defaults here.
                    athStitcherViewModel.SetSelectedStartTime(timeFromGunStart);
                }
            }
            else
            {
                //FinishTime.Text = "";
                FinishTime.Text = $"{timeFromGunStart:F2}";
                //TimeLabel.Text = $"{timeInSeconds:F2}";
                TimeLabel.Visibility = Visibility.Collapsed;
                Clipboard.SetData(DataFormats.Text, (Object)"");
            }
        }


        private void StitchedImage_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }


        ////////////////////////////////////// File Menu //////////////////////////////////////

        /// <summary>
        /// Select the MP4 file but not open it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenMp4File_Click(object sender, RoutedEventArgs e)
        {
            string videoFilePath = athStitcherViewModel.GetVideoPath();
            OpenFileDialog openFileDialog;
            if (File.Exists(videoFilePath))
            {
                string? initialDirectory = System.IO.Path.GetDirectoryName(videoFilePath);
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "MP4 Files (*.mp4)|*.mp4",
                        InitialDirectory = initialDirectory, // Set the folder
                        FileName = System.IO.Path.GetFileName(videoFilePath) //videoFilePath
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
                athStitcherViewModel.SetVideoPath(videoFilePath);
                //string pattern = @"_GUN_(\d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$";
                string wallClockPattern = @"_WALL_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$";
                string gunPattern = @"_GUN\.mp4$";
                string flashPattern = @"_FLASH\.mp4$";
                string manualPattern = @"_MAN\.mp4$";

                string imagePath = Regex.Replace(videoFilePath, ".mp4", ".png", RegexOptions.IgnoreCase);

                // Match the video file path against the patterns
                //WallClock in filename
                Match match = Regex.Match(videoFilePath, wallClockPattern,RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    imagePath = Regex.Replace(videoFilePath, wallClockPattern, ".png", RegexOptions.IgnoreCase);

                    string gunTimeString = match.Groups[1].Value;

                    // Normalize by replacing "--" with ":" in time portion
                    int timeStartIndex = gunTimeString.IndexOf(' ') + 1;
                    string normalized = gunTimeString.Substring(0, timeStartIndex) +
                                        gunTimeString.Substring(timeStartIndex).Replace("--", ":");

                    DateTime gunDateTime = DateTime.ParseExact(normalized, "yyyy-MM-dd HH:mm:ss.fff", null);
                    Console.WriteLine($"Parsed DateTime: {gunDateTime}");
                    athStitcherViewModel.SetEventWallClockStartTime(gunDateTime);
                    athStitcherViewModel.SetTimeFromMode(TimeFromMode.WallClockSelect); // Set the mode to WallClockSelect
                }
                else
                {
                    // Format: <Filename>_gun.mp4 or <Filename>_gun.mp4 etc.
                    match = Regex.Match(videoFilePath, gunPattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        imagePath = Regex.Replace(videoFilePath, gunPattern, ".png", RegexOptions.IgnoreCase);
                        athStitcherViewModel.SetTimeFromMode(TimeFromMode.FromGunviaAudio); // Set the mode to WallClockSelect
                    }
                    else
                    {
                        match = Regex.Match(videoFilePath, flashPattern, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            imagePath = Regex.Replace(videoFilePath, flashPattern, ".png", RegexOptions.IgnoreCase);
                            athStitcherViewModel.SetTimeFromMode(TimeFromMode.FromGunViaVideo); // Set the mode to WallClockSelect
                        }
                        else
                        {
                            // Format: <Filename>_flash.mp4 or <Filename>_flash.mp4 etc.
                            match = Regex.Match(videoFilePath, manualPattern, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                imagePath = Regex.Replace(videoFilePath, manualPattern, ".png", RegexOptions.IgnoreCase);
                                DateTime videoCreationDate = athStitcherViewModel.GetVideoCreationDate();
                                athStitcherViewModel.SetEventWallClockStartTime(videoCreationDate);
                                athStitcherViewModel.SetTimeFromMode(TimeFromMode.ManuallySelect); // Set the mode to WallClockSelect
                            }
                            else
                            {
                                // No match found, suse what was eslected on the Menu
                                Console.WriteLine($"No match found.");
                                
                                imagePath = Regex.Replace(videoFilePath, ".mp4", ".png", RegexOptions.IgnoreCase);
                                DateTime videoCreationDate = athStitcherViewModel.GetVideoCreationDate();
                                athStitcherViewModel.SetEventWallClockStartTime(videoCreationDate);
                            }
                        }
                    }
                }
                athStitcherViewModel.SetOutputPath(imagePath);
                StitchButton_Click(this, e);
                if(athStitcherViewModel.GetTimeFromMode() == TimeFromMode.WallClockSelect)
                {
                    //Ok_Click(this, e);
                }

            }
        }

        /// <summary>
        /// Select the Stitched Video output PNG file and open if it exists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenPngFile_Click(object sender, RoutedEventArgs e)
        {
            string OutputFilePath = athStitcherViewModel.GetOutputPath();
            OpenFileDialog openFileDialog;
            if (File.Exists(OutputFilePath))
            {
                string? initialDirectory = System.IO.Path.GetDirectoryName(OutputFilePath);
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "PNG Files (*.png)|*.png",
                        InitialDirectory = initialDirectory, // Set the folder
                        FileName = System.IO.Path.GetFileName(OutputFilePath) //OutputFilePath
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
                athStitcherViewModel.SetOutputPath(OutputFilePath); // Update the ViewModel with the new path
                if (File.Exists(OutputFilePath))
                {
                    LoadImageButton_Click(null, null);
                    // If the file name contains "_Start_", set the time from mode to ManuallySelect
                    if (OutputFilePath.Contains("_Start_", StringComparison.OrdinalIgnoreCase))
                    {
                        //using System.Text.RegularExpressions;
                        //var match = Regex.Match(OutputFilePath, @"[-+]?\d*\.\d+|\d+");
                        var match = Regex.Match(OutputFilePath, @"_start_([-+]?\d*\.?\d+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var numberString = match.Groups[1].Value; // just the numeric portion
                            if (double.TryParse(numberString, out double dbl))
                            {
                                athStitcherViewModel.SetGunTime(dbl, 0); // Set the gun time in the ViewModel
                                athStitcherViewModel.SetTimeFromMode(TimeFromMode.ManuallySelect);
                                athStitcherViewModel.SetHasStitched();
                                athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Failed to select a PNG image file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Select the Gun Audio text file but not open it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenGunAudioTextFile_Click(object sender, RoutedEventArgs e)
        {
            string GunAudioPathInput = athStitcherViewModel.GetGunAudioPath();
            OpenFileDialog openFileDialog;
            if (File.Exists(GunAudioPathInput))
            {
                string? initialDirectory = System.IO.Path.GetDirectoryName(GunAudioPathInput);
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                {
                    openFileDialog = new OpenFileDialog
                    {
                        Filter = "TXT Files (*.txt)|*.txt",
                        InitialDirectory = initialDirectory, // Set the folder
                        FileName = System.IO.Path.GetFileName(GunAudioPathInput)
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
                athStitcherViewModel.SetGunAudioPath(GunAudioPathInput); // Update the ViewModel with the new path
            }
        }

        //////////////////////// Time from and Video Detect Mode Menu Handlers ////////////////

        /// <summary>
        /// From menu set the TimeFromMode property in the ViewModel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeFromMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is TimeFromMode timeMode)
            {
                athStitcherViewModel.SetTimeFromMode(timeMode);
            }
        }

        /// <summary>
        /// Set the VideoDetectMode property in the ViewModel from the menu 
        /// ... when TimeFromMode is set to FromGunViaVideo or FromFlash.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoDetectMode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is VideoDetectMode detectMode)
            {
                // Update the view model's VideoDetectMode property.
                athStitcherViewModel.SetVideoDetectMode(detectMode);
            }
        }


        ///////////////////////////// MyViewModel State Management /////////////////////////////


        /// <summary>
        /// Called from File Menu
        /// Shouldn't be needed because the ViewModel is saved automatically on property change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveViewModel_Click(object sender, RoutedEventArgs e)
        {
            athStitcherViewModel.SaveViewModel();
        }

        private void DisplayFrame(int frameNo, double posX, bool resize = true)
        {

            // Get mouse position relative to the container

            if ((videoStitcher == null))
            {
                return;
            }
            Bitmap bitmap = videoStitcher.GetNthFrame(frameNo);

            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            FrameImage.Source = bitmapImage;
            //StitchedImage.Source = bitmapImage;
            //if (StitchedImage.Source is BitmapSource bitmapx)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Image Dimensions: {bitmapx.PixelWidth}x{bitmap.PixelHeight}");
            //}
            resize = false;
            if (PopupVideoFrameImage.Width is double.NaN)
                resize = true;
            if (resize)
            {

                FrameImage.Width = 100;
                //FrameImage.Height = 100;
                var width = FrameImage.Source.Width;
                var height = FrameImage.Source.Height;
                double ratio = height / width;
                FrameImage.Height = ratio * FrameImage.Width + ResizeThumb.Height;
            }
            PopupVideoFrameImage.Width = FrameImage.Width;
            PopupVideoFrameImage.Height = FrameImage.Height;
            PopupVideoFrameImage.HorizontalOffset = FrameImage.Width / 2;
            Divider.Y2 = FrameImage.Height;
           
            //PopupVideoFrameImage.HorizontalOffset =  (int)(PopupVideoFrameImage.Width / 2); // GetPopupWidth();
            //PopupVideoFrameImage.VerticalOffset = 0;// (int)PopupVideoFrameImage.Height; /*GetTimeLabelMargin().Top + TimeLabel.ActualHeight +115;*/

            PopupVideoFrameImage.IsOpen = true;
            return;
        }

        private void NudgeDisplayFrame(int frameNo, double posX, bool resize = true)
        {

            // Get mouse position relative to the container

            if ((videoStitcher == null))
            {
                return;
            }
            Bitmap bitmap = videoStitcher.GetNthFrame(frameNo);

            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            NudgeFrameImage.Source = bitmapImage;
            //StitchedImage.Source = bitmapImage;
            //if (StitchedImage.Source is BitmapSource bitmapx)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Image Dimensions: {bitmapx.PixelWidth}x{bitmap.PixelHeight}");
            //}
            resize = false;
            if (NudgePopupVideoFrameImage.Width is double.NaN)
                resize = true;
            if (resize)
            {

                NudgeFrameImage.Width = 100;
                //FrameImage.Height = 100;
                var width = NudgeFrameImage.Source.Width;
                var height = NudgeFrameImage.Source.Height;
                double ratio = height / width;
                NudgeFrameImage.Height = ratio * NudgeFrameImage.Width + ResizeThumb.Height;
            }
            NudgePopupVideoFrameImage.Width = NudgeFrameImage.Width;
            NudgePopupVideoFrameImage.Height = NudgeFrameImage.Height;
            NudgePopupVideoFrameImage.HorizontalOffset = 0;// NudgeFrameImage.Width / 2;
            NudgeDivider.Y2 = NudgeFrameImage.Height;

            //PopupVideoFrameImage.HorizontalOffset =  (int)(PopupVideoFrameImage.Width / 2); // GetPopupWidth();
            //PopupVideoFrameImage.VerticalOffset = 0;// (int)PopupVideoFrameImage.Height; /*GetTimeLabelMargin().Top + TimeLabel.ActualHeight +115;*/

            NudgePopupVideoFrameImage.IsOpen = true;
            return;
        }

        private void LoadStitchedImage(string imageFilePath)
        {
            BitmapImage bitmap = new BitmapImage();
            if (!File.Exists(imageFilePath))
            {
                MessageBox.Show($"The specified image file does not exist: {imageFilePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            using var fs = new FileStream(imageFilePath, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // read into RAM, release stream afterwards
            bitmap.StreamSource = fs;                      // <— no Uri, so no cache
            bitmap.EndInit();
            bitmap.Freeze();

            StitchedImage.Source = bitmap;
            if (StitchedImage.Source is BitmapSource bitmapx)
            {
                System.Diagnostics.Debug.WriteLine($"Image Dimensions: {bitmapx.PixelWidth}x{bitmap.PixelHeight}");
            }
            return;
        }
        private void WriteGunLineButton_Click(object sender, RoutedEventArgs e)
        {
            if (!athStitcherViewModel.IsDataContext())
                return;
            selectedStartTime = athStitcherViewModel.GetSelectedStartTime();
            WriteGLine(selectedStartTime);
        }

        private void WriteGLine(double guntime, int gunTimeIndex = -1)
        {
            if (!athStitcherViewModel.IsDataContext())
                return;

            // Hide lines
            StartVerticalLine.Visibility = Visibility.Collapsed;
            VerticalLine.Visibility = Visibility.Collapsed;
            TimeLabel.Visibility = Visibility.Collapsed;

            //selectedStartTime = this.GetSelectedStartTime();
            string outputPath = athStitcherViewModel.GetOutputPath();
            // Only write line if non zero start time is selected
            if (guntime != 0)
            {
                if (videoStitcher == null)
                {
                    videoStitcher = new PhotoTimingDjaus.VideoStitcher(
                       athStitcherViewModel.GetVideoPath(),
                       athStitcherViewModel.GetGunColor(),
                       athStitcherViewModel.GetOutputPath(),
                       guntime,
                       100, //axisHeight,
                       100, //audioHeight,
                       athStitcherViewModel.GetlevelImage(),
                       athStitcherViewModel.GetTimeFromMode(),
                       threshold);
                }

                if (gunTimeIndex <= 0)
                    gunTimeIndex = videoStitcher.AddGunLine(guntime, athStitcherViewModel.GetGunColor());

                LoadStitchedImage(athStitcherViewModel.GetOutputPath());

                athStitcherViewModel.SetGunTime(guntime, gunTimeIndex);
                athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                var a = athStitcherViewModel.Get_HasStitched();
                var b = athStitcherViewModel.Get_HaveSelectedandShownGunLineinManualorWallClockMode();
                var c = athStitcherViewModel.GetTimeFromMode();

                MessageBox.Show("Stitched image successfully updated and displayed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        double horizOffset = 0;
        double horizOffsetz = 0;
        double verticalOffset = 0;

        /// <summary>
        /// Nudge SelectedStartTime +/- by 1/100 second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NudgeButton_Click(object sender, RoutedEventArgs e)
        {
            var prevline = (StartVerticalLine.Visibility == Visibility.Visible)
            ? StartVerticalLine
            : (VerticalLine.Visibility == Visibility.Visible) ? VerticalLine
            : (NudgeVerticalLine.Visibility == Visibility.Visible) ? NudgeVerticalLine : null;
            if (prevline == null)
                return;

            if(prevline != NudgeVerticalLine)
            {
                prevline.Visibility = Visibility.Collapsed;
                NudgeVerticalLine.X1 = prevline.X1;
                NudgeVerticalLine.X2 = prevline.X2;
                NudgeVerticalLine.Y1 = prevline.Y1;
                NudgeVerticalLine.Y2 = prevline.Y2;
                NudgeVerticalLine.Visibility = Visibility.Visible;
                horizOffsetz = NudgeVerticalLine.X1;
            }
            if (sender is Button button)
            {
                string toolTip = button.ToolTip?.ToString() ?? "";
                if(!toolTip.Contains("WC"))
                    Nudge(toolTip);
                else 
                    NudgeWC(toolTip);
            }
            ImageCanvas.UpdateLayout();
            NudgeVerticalLine.UpdateLayout();
            horizOffset = NudgeVerticalLine.X1- horizOffsetz;

            PositionPopupOverLine();
            NudgePopupVideoFrameImage.UpdateLayout();
        }

        /// <summary>
        /// Nudge SelectedStartTime +/- by 1/100 second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Nudge(string toolTip)
        {

            PopupVideoFrameImage.IsOpen = false;
            if (toolTip == "")
                return;
            StartVerticalLine.Visibility = Visibility.Collapsed;
            VerticalLine.Visibility = Visibility.Collapsed;
            System.Windows.Shapes.Line _VerticalLine = NudgeVerticalLine;
            TimeFromMode timeFromMode = athStitcherViewModel.GetTimeFromMode();
            double startTime = 0;
            bool isManualNotSelected = false;
            bool isLeft = true;
            double posX;
            if (timeFromMode == TimeFromMode.ManuallySelect)
            {
                var hsnshwngunline = athStitcherViewModel.Get_HaveSelectedandShownGunLineinManualorWallClockMode();
                if (!hsnshwngunline)
                {
                    _VerticalLine = StartVerticalLine; // Use the start line for manual mode
                    _VerticalLine.Visibility = Visibility.Visible;
                    NudgeVerticalLine.Visibility = Visibility.Collapsed;
                    selectedStartTime = athStitcherViewModel.GetSelectedStartTime();
                    startTime = selectedStartTime;
                    isManualNotSelected = true;
                    isLeft = false;
                }
            }
            //if (_VerticalLine.Visibility == Visibility.Collapsed)
            //_VerticalLine.Visibility = Visibility.Visible;
            ////return;
            double horizontalScale = 1;
            double verticalScale = 1;
            if (StitchedImage.LayoutTransform is ScaleTransform transform)
            {
                horizontalScale = transform.ScaleX; // Get the horizontal scale
                verticalScale = transform.ScaleY;
                //posX = position.X * horizontalScale; // Adjust time based on scale
            }
            double stitchedImageVirtualWidth = StitchedImage.ActualWidth * horizontalScale;
            double stitchedImageVirtualHeight = StitchedImage.ActualHeight * verticalScale;

            posX = _VerticalLine.X1;
            double oneFrame = 1 / Fps;
            double oneSecNoFrames = Fps;
            videoLength = athStitcherViewModel.GetVideoLength();
            int numFrames = (int)(videoLength * Fps);
            double posXPrev = posX;
            if (toolTip == "Back")
            {
                if (posX >= 1* horizontalScale)
                {
                    //Back one Frame
                    posX -= 1* horizontalScale;
                }
            }
            else if (toolTip == "Forward")
            {
                if (posX <= (stitchedImageVirtualWidth - 1* horizontalScale))
                {
                    //Forward one Frame
                    posX += 1* horizontalScale;
                }
            }
            else if (toolTip == "Back 5")
            {
                if (posX >= 5* horizontalScale)
                {
                    //Back five Frames
                    posX -= 5 * horizontalScale;
                }
            }
            else if (toolTip == "Forward 5")
            {
                if (posX < (stitchedImageVirtualWidth - 5* horizontalScale))
                {
                    //Forward five Frames
                    posX += 5* horizontalScale;
                }
            }
            else if (toolTip == "Back 1 sec")
            {
                if (posX >= oneSecNoFrames* horizontalScale)
                {
                    //Back five Frames
                    posX -= oneSecNoFrames* horizontalScale;
                }
            }
            else if (toolTip == "Forward 1 sec")
            {
                if (posX < (stitchedImageVirtualWidth - oneSecNoFrames* horizontalScale))
                {
                    //Forward five Frames
                    posX += oneSecNoFrames* horizontalScale;
                }
            }

            if (posX == posXPrev)
            {
                // No change in time, so do not update the line or label
                return;
            }

            startTime = (posX / stitchedImageVirtualWidth) * videoLength;
            GunTimeDbl = athStitcherViewModel.GetGunTime(); // Get the gun time from the ViewModel
            if (startTime < GunTimeDbl)
                return;


            string formattedTime = $"{startTime}";                                                                                                      // Display the calculated time
            TimeLabel.Visibility = Visibility.Visible;
            TimeLabel.Text = $"{startTime:F2} sec";
            var margin = athStitcherViewModel.GetTimeLabelMargin();
            margin.Left += posX;
            TimeLabel.Margin = margin;

            _VerticalLine.X1 = posX;
            _VerticalLine.X2 = posX;
            _VerticalLine.Y1 = 0; // Top of the image
            double posY2 = stitchedImageVirtualHeight;
            
            _VerticalLine.Y2 = posY2; // Bottom of the image

            horizOffset = posX- horizOffset;
            verticalOffset = posY2; ;
            UpdateTimeLabel(posX,startTime,isLeft);
            if (athStitcherViewModel.GetShowVideoFramePopup())
                NudgeDisplayFrame(frameNo, posX, false);
        }

        /// <summary>
        /// Nudge WallClock +/- by 1/100 second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NudgeWC(string toolTip)
        {

            if (toolTip == "")
                return;
            TimeFromMode timeFromMode = athStitcherViewModel.GetTimeFromMode();
            if (timeFromMode != TimeFromMode.WallClockSelect)
                return;
            TimeSpan eventStartWallClockTimeofDay = athStitcherViewModel.GetEventWallClockStartTimeofDay();
            var WClockTime = eventStartWallClockTimeofDay;

            TimeSpan oneSec = new TimeSpan(0, 0, 0, 1);
            TimeSpan fiveFramesTs = new TimeSpan(0, 0, 0, 0, 0, (int)Math.Round(5000000 / Fps, 0));
            TimeSpan oneFrameTs = new TimeSpan(0, 0, 0, 0, 0, (int)Math.Round(1000000 / Fps,0));

            if (toolTip == "WC Back 1 Frame")
            {
                //Back one Frame
                eventStartWallClockTimeofDay = eventStartWallClockTimeofDay.Subtract(oneFrameTs);
            }
            else if (toolTip == "WC Forward 1 Frame")
            {
                eventStartWallClockTimeofDay = eventStartWallClockTimeofDay.Add(oneFrameTs);
            }
            if (toolTip == "WC Back 5 Frames")
            {
                //Back one Frame
                eventStartWallClockTimeofDay = eventStartWallClockTimeofDay.Subtract(fiveFramesTs);
            }
            else if (toolTip == "WC Forward 5 Frames")
            {
                eventStartWallClockTimeofDay = eventStartWallClockTimeofDay.Add(fiveFramesTs);
            }
            else if (toolTip == "WC Back 1 sec")
            {
                eventStartWallClockTimeofDay = eventStartWallClockTimeofDay.Subtract(oneSec);
            }
            else if (toolTip == "WC Forward 1 sec")
            {
                eventStartWallClockTimeofDay = eventStartWallClockTimeofDay.Add(oneSec);
            }

            if (eventStartWallClockTimeofDay == WClockTime)
            {
                // No change in time, so do not update
                return;
            }
            athStitcherViewModel.SetEventWallClockStartTimeofDay(eventStartWallClockTimeofDay);
        }

        private void ResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var margin = PopupVideoFrameImage.Margin;

            double prevWidth = FrameImage.Width;
            double posx = PopupVideoFrameImage.HorizontalOffset - prevWidth;
            var width = FrameImage.Source.Width;
            var height = FrameImage.Source.Height;
            double ratio = height / width;
            double newWidth = FrameImage.Width + e.HorizontalChange;
            double newHeight = ratio * FrameImage.Width + ResizeThumb.Height;

            // Ensure minimum size
            FrameImage.Width = Math.Max(newWidth, athStitcherViewModel.GetMinPopupWidth() / 2);
            FrameImage.Height = Math.Max(newHeight, athStitcherViewModel.GetMinPopupHeight() / 2);
            PopupVideoFrameImage.Width = FrameImage.Width;
            PopupVideoFrameImage.Height = FrameImage.Height;
            posx += FrameImage.Width; ;
            PopupVideoFrameImage.HorizontalOffset = posx; // Update horizontal offset to keep the popup in place

            // Close popup if resized below 50px
            if (FrameImage.Width <= (athStitcherViewModel.GetMinPopupWidth() / 2) || FrameImage.Height <= (athStitcherViewModel.GetMinPopupHeight() / 2))
            {
                PopupVideoFrameImage.IsOpen = false;
            }
        }

        private void NudgeResizeThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var margin = NudgePopupVideoFrameImage.Margin;

            double prevWidth = NudgeFrameImage.Width;
            double posx = NudgePopupVideoFrameImage.HorizontalOffset - prevWidth;
            var width = NudgeFrameImage.Source.Width;
            var height = NudgeFrameImage.Source.Height;
            double ratio = height / width;
            double newWidth = NudgeFrameImage.Width + e.HorizontalChange;
            double newHeight = ratio * NudgeFrameImage.Width + ResizeThumb.Height;

            // Ensure minimum size
            NudgeFrameImage.Width = Math.Max(newWidth, athStitcherViewModel.GetMinPopupWidth() / 2);
            NudgeFrameImage.Height = Math.Max(newHeight, athStitcherViewModel.GetMinPopupHeight() / 2);
            NudgePopupVideoFrameImage.Width = FrameImage.Width;
            NudgePopupVideoFrameImage.Height = FrameImage.Height;
            posx += NudgeFrameImage.Width; ;
            NudgePopupVideoFrameImage.HorizontalOffset = posx; // Update horizontal offset to keep the popup in place

            // Close popup if resized below 50px
            if (NudgeFrameImage.Width <= (athStitcherViewModel.GetMinPopupWidth() / 2) || NudgeFrameImage.Height <= (athStitcherViewModel.GetMinPopupHeight() / 2))
            {
                NudgePopupVideoFrameImage.IsOpen = false;
            }
        }

        private void Popup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bool isShift = Keyboard.IsKeyDown(Key.LeftShift);
            if (e.ClickCount == 1) // Detect double-click
            {
                if (isShift)
                {
                    FrameImage.Width /= 1.5; 
                    FrameImage.Height /= 1.5;
                    PopupVideoFrameImage.Width /= 1.5;
                    PopupVideoFrameImage.Height /= 1.5;
                    if (FrameImage.Width < athStitcherViewModel.GetMinPopupWidth() / 2 || FrameImage.Height < athStitcherViewModel.GetMinPopupHeight() / 2)
                    {
                        PopupVideoFrameImage.IsOpen = false; // Close popup if too small
                    }
                }
                else 
                { 
                    FrameImage.Width *= 1.5; // Close popup
                    FrameImage.Height *= 1.5;
                    PopupVideoFrameImage.Width *= 1.5;
                    PopupVideoFrameImage.Height *= 1.5;
                }
            }
            else if (e.ClickCount == 2) // Detect double-click
            {
                PopupVideoFrameImage.IsOpen = false; // Close popup
            }
        }

        private void NudgePopup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            bool isShift = Keyboard.IsKeyDown(Key.LeftShift);
            if (e.ClickCount == 1) // Detect double-click
            {
                if (isShift)
                {
                    NudgeFrameImage.Width /= 1.5;
                    NudgeFrameImage.Height /= 1.5;
                    NudgePopupVideoFrameImage.Width /= 1.5;
                    NudgePopupVideoFrameImage.Height /= 1.5;
                    if (NudgeFrameImage.Width < athStitcherViewModel.GetMinPopupWidth() / 2 || NudgeFrameImage.Height < athStitcherViewModel.GetMinPopupHeight() / 2)
                    {
                        NudgePopupVideoFrameImage.IsOpen = false; // Close popup if too small
                    }
                }
                else
                {
                    NudgeFrameImage.Width *= 1.5; // Close popup
                    NudgeFrameImage.Height *= 1.5;
                    NudgePopupVideoFrameImage.Width *= 1.5;
                    NudgePopupVideoFrameImage.Height *= 1.5;
                }
            }
            else if (e.ClickCount == 2) // Detect double-click
            {
                NudgePopupVideoFrameImage.IsOpen = false; // Close popup
            }
        }

        private void ImageKeyDown(object sender, KeyEventArgs e)
        {
            if (!athStitcherViewModel.IsDataContext())
                return;
            if (!athStitcherViewModel.Get_HasStitched())
                return;
            // Check if the pressed key is Escape
            bool shift = false;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                shift = true;
            }
            switch (e.Key)
            {
                case Key.Left:
                    if (shift)
                    {
                        Nudge("Back 5");
                    }
                    else
                    {
                        Nudge("Back");
                    }
                    Nudge("Back");
                    break;
                case Key.Right:
                    if (shift)
                    {
                        Nudge("Forward 5");
                    }
                    else
                    {
                        Nudge("Forward");
                    }
                    Nudge("Forward");
                    break;
                default:
                    break;
            }

        }


        private void ShowPopup(object s, RoutedEventArgs e)
        {
            Dp.DisplayDate = DateTime.Now;       // reset default
            WatchClockDateTimePopup.IsOpen = true;
        }

        private void Ok_Click(object s, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.AthStitcherModel viewModel)
            {

                var dt = viewModel.EventStartWallClockDateTime;
                var timeofDay = dt.TimeOfDay;
                var videoStartTime = viewModel.VideoCreationDate;
                TimeSpan timeSpan = timeofDay - videoStartTime.TimeOfDay;
                double gunTime = timeSpan.TotalSeconds;
                if (videoStitcher == null)
                {
                    videoStitcher = new PhotoTimingDjaus.VideoStitcher(
                        athStitcherViewModel.GetVideoPath(),
                        athStitcherViewModel.GetGunColor(),
                        athStitcherViewModel.GetOutputPath(),
                        athStitcherViewModel.GetSelectedStartTime(),
                        100, //axisHeight,
                        100, //audioHeight,
                        athStitcherViewModel.GetlevelImage(),
                        athStitcherViewModel.GetTimeFromMode(),
                        threshold);
                }
                this.WriteGLine(gunTime);
            }

            WatchClockDateTimePopup.IsOpen = false;
        }

        private void Cancel_Click(object s, RoutedEventArgs e)
        {
            WatchClockDateTimePopup.IsOpen = false;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Photo Timing Djaus\nVersion 1.0\n\nA tool for stitching video frames and timing gun shots.\n\nDeveloped by David Jones\n\nBlog: https://davidjones.sportronics.com.au\n\nRepository https://github.com/djaus2/PhotoTimingDjaus", "About Photo Timing Djaus", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BlogSite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://davidjones.sportronics.com.au",
                UseShellExecute = true
            });
        }

        private void Repo_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/djaus2/PhotoTimingDjaus",
                UseShellExecute = true
            });
        }

        private void NuGet_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.nuget.org/packages/djaus2_MauiMediaRecorderVideoLib/",
                UseShellExecute = true
            });
        }
        private void AndroidAppNuGet_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/djaus2/MauiMediaRecorderVideoAndroidApp\r\n",
                UseShellExecute = true
            });
        }

        private void PositionPopupOverLine1()
        {
            var line = (StartVerticalLine.Visibility == Visibility.Visible)
                ? StartVerticalLine
                : (VerticalLine.Visibility == Visibility.Visible) ? VerticalLine
                : (NudgeVerticalLine.Visibility == Visibility.Visible) ? NudgeVerticalLine : null;


            if (line == null) return;

            var point = line.TransformToAncestor(ImageCanvas).Transform(new System.Windows.Point(0, 0));
            PopupVideoFrameImage.HorizontalOffset = point.X - PopupVideoFrameImage.ActualWidth / 2;
            PopupVideoFrameImage.VerticalOffset = point.Y - PopupVideoFrameImage.ActualHeight - 10;
            PopupVideoFrameImage.IsOpen = true;
        }

        private void PositionPopupOverLine(bool resize = false)
        {
            if (!athStitcherViewModel.IsDataContext())
                return;

            Line? lineToUse = NudgeVerticalLine;
            if(NudgeVerticalLine.Visibility== Visibility.Collapsed)
            {
                if(StartVerticalLine.Visibility== Visibility.Visible)
                {
                    lineToUse = StartVerticalLine;
                }
                else
                {
                    return;
                }
            }
            

            if (lineToUse != null)
            {
                if (lineToUse.Visibility != Visibility.Visible)
                    return;

                if(NudgePopupVideoFrameImage.Width < athStitcherViewModel.GetMinPopupWidth()/2)
                {
                    resize = true;
                }
                else if (NudgePopupVideoFrameImage.Width is double.NaN)
                    resize = true;
                if (resize)
                {

                    NudgeFrameImage.Width = 100;
                    //FrameImage.Height = 100;
                }
                var width = NudgeFrameImage.Source.Width;
                var height = NudgeFrameImage.Source.Height;
                double ratio = height / width;
                NudgeFrameImage.Height = ratio * NudgeFrameImage.Width;
                NudgePopupVideoFrameImage.Width = NudgeFrameImage.Width;
                NudgePopupVideoFrameImage.Height = NudgeFrameImage.Height + NudgeResizeThumb.Height;
                
                System.Windows.Point fakeMousePoint = new System.Windows.Point(horizOffset+100+ athStitcherViewModel.GetGunTime(), 0); // arbitrarily chosen coordinates
                var screenPoint = ImageCanvas.PointToScreen(fakeMousePoint);
                var windowPoint = this.PointFromScreen(screenPoint);
                if (NudgePopupVideoFrameImage.VerticalOffset <= 0)
                    NudgePopupVideoFrameImage.VerticalOffset = 100;
                NudgePopupVideoFrameImage.HorizontalOffset = 0;
                if (DataContext is ViewModels.AthStitcherModel MyViewModel)
                {
                    var mode = MyViewModel.PopupPlacement;

                    ratio = 1 / HorizontalZoomSlider.Value;

                    switch (mode)
                    {
                        case PlacementMode.Left:
                            NudgePopupVideoFrameImage.HorizontalOffset = -ratio * NudgePopupVideoFrameImage.Width ;
                            NudgePopupVideoFrameImage.VerticalOffset = 100;
                            break;
                        case PlacementMode.Right:
                            NudgePopupVideoFrameImage.HorizontalOffset = ratio * NudgePopupVideoFrameImage.Width;
                            NudgePopupVideoFrameImage.VerticalOffset = 100;
                            break;
                        case PlacementMode.Center:
                            NudgePopupVideoFrameImage.HorizontalOffset = 0; // -NudgePopupVideoFrameImage.Width
                            NudgePopupVideoFrameImage.VerticalOffset = 0;
                            break;
                        case PlacementMode.Bottom:
                            NudgePopupVideoFrameImage.HorizontalOffset = 0; // -NudgePopupVideoFrameImage.Width
                            NudgePopupVideoFrameImage.VerticalOffset = 0;
                            break;
                    }
                    // -NudgePopupVideoFrameImage.Width; // -StitchedImage.ActualWidth/2;// 0; // (int)Math.Round(windowPoint.X  ,0);

                    //NudgePopupVideoFrameImage.VerticalOffset = 0;
                    System.Diagnostics.Debug.WriteLine(horizOffset);
                }

                return;
            }
        }

        private void TruncateandSelectVideoFile_Click(object sender, RoutedEventArgs e)
        {
            string videoFilePath = athStitcherViewModel.GetVideoPath();
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
            else
            {
                openFileDialog = new OpenFileDialog
                {
                    Filter = "MP4 Files (*.mp4)|*.mp4",
                };
            }

            if (openFileDialog.ShowDialog() == true)
            {
                videoFilePath = openFileDialog.FileName;
                athStitcherViewModel.SetVideoPath(videoFilePath);
                //string pattern = @"_GUN_(\d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$";
                string pattern = @"_GUN_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$";

                Match match = Regex.Match(videoFilePath, pattern);
                if (match.Success)
                {
                    string gunTimeString = match.Groups[1].Value;

                    // Normalize by replacing "--" with ":" in time portion
                    int timeStartIndex = gunTimeString.IndexOf(' ') + 1;
                    string normalized = gunTimeString.Substring(0, timeStartIndex) +
                                        gunTimeString.Substring(timeStartIndex).Replace("--", ":");

                    DateTime gunDateTime = DateTime.ParseExact(normalized, "yyyy-MM-dd HH:mm:ss.fff", null);
                    Console.WriteLine($"Parsed DateTime: {gunDateTime}");
                    athStitcherViewModel.SetEventWallClockStartTime(gunDateTime);
                    athStitcherViewModel.SetTimeFromMode(TimeFromMode.WallClockSelect); // Set the mode to WallClockSelect
                }
                else
                {
                    Console.WriteLine("No match found.");
                    athStitcherViewModel.SetEventWallClockStartTime(DateTime.MinValue);
                    athStitcherViewModel.SetTimeFromMode(TimeFromMode.ManuallySelect); // Set the mode to WallClockSelect
                }
            }
        }


    }

    ////////////////////////////////////////////////////////////////////////////////////////
}
