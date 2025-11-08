using AthStitcher.Data;
using AthStitcher.Security;
using AthStitcher.Views;
using AthStitcherGUI.ViewModels;
using Castle.Core.Resource;
using DetectAudioFlash;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

//using OpenCvSharp;
//using OpenCvSharp;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using NAudio.Utils;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using PhotoTimingDjaus;
using PhotoTimingDjausLib;
using SharpVectors.Converters;
using SharpVectors.Dom.Events;
using Sportronics.VideoEnums; //.Local;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;


namespace AthStitcherGUI
{

    public partial class MainWindow : System.Windows.Window
    {
        string _EXIFTOOL = "exiftool";
        string _EXIFTOOLEXE = "exiftool(-k).exe";
        // Temporary: force-create database on startup (set to false later)
        private const bool CreateDatabaseOnStart = true;

        private PhotoTimingDjaus.VideoStitcher? videoStitcher = null;
        //private int margin = 20;
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
        private bool HaveGotGunTime = false;

        // Track last result lane index and whether we've pasted at least once
        private int? _lastResultLaneIndex = null; // 1-based lane index from Result1..Result9
        private bool _hasPastedAnyResult = false;
        // Flags for current result lifecycle
        private bool _hasNewResultAvailable = false;   // Set true when user releases on stitched image
        private int? _currentResultLaneIndex = null;   // Where the current result is placed; cleared on new image click

        public MainWindow()
        {
            InitializeComponent();
            athStitcherViewModel = new AthStitcherViewModel();
            this.DataContext = viewModel;
            // Capture mouse clicks to enable paste-on-click into Results grid
            ResultsDataGrid.AddHandler(System.Windows.Controls.DataGrid.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(ResultsGrid_PreviewMouseLeftButtonDown), true);
            // Initialize database via EF and ensure Admin user exists
            try
            {
                athStitcherViewModel.LoadViewModel();
                this.DataContext = athStitcherViewModel.DataContext; // Set the DataContext to the AthStitchView instance

                if (string.IsNullOrEmpty(athStitcherViewModel.DataContext.ExifTool))
                    athStitcherViewModel.DataContext.ExifTool = _EXIFTOOL;
                if (string.IsNullOrEmpty(athStitcherViewModel.DataContext.ExifToolExe))
                    athStitcherViewModel.DataContext.ExifToolExe = _EXIFTOOLEXE;

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

                // Defer EF migration to after window loads, off UI thread to avoid startup hang
                this.Loaded += async (_, __) =>
                {
                    try
                    {
                        await Task.Run(() =>
                        {

                            using var ctx = new AthStitcher.Data.AthStitcherDbContext();
                            //ctx.Database.EnsureDeleted();
                            ctx.Database.EnsureCreated();
                            //ctx.Database.Migrate();
                        });
                        // Seeding uses short operations; OK on UI thread post-migrate
                        SeedAdminIfMissing();
                        EnforceForcePasswordChangeIfNeeded();
                    }
                    catch (Exception ex2)
                    {
                        MessageBox.Show($"Database initialization error: {ex2.Message}", "Database", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization error: {ex.Message}", "Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Menu handler to delete and recreate database (for schema changes during development)
        private void DeleteAndRecreateDatabase_Menu_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "This will delete the local AthStitcher database file and recreate it. Continue?",
                "Delete Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                using var ctx = new AthStitcher.Data.AthStitcherDbContext();
                if (!ctx.Database.EnsureDeleted())
                {
                    // If ensure delete returns false or fails due to locks, try to drop all tables then continue
                    try { ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;"); }
                    catch { }
                    try
                    {
                        ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Results;");
                        ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Events;");
                        ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Meets;");
                        ctx.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Users;");
                    }
                    catch { }
                    try { ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;"); }
                    catch { }
                }
                ctx.Database.Migrate();
                SeedAdminIfMissing();
                MessageBox.Show("Database recreated successfully.", "Database", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete/recreate database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SeedAdminIfMissing()
        {
            using var ctx = new AthStitcher.Data.AthStitcherDbContext();
            var admin = ctx.Users.SingleOrDefault(u => u.Username == "admin");
            if (admin == null)
            {
                string tempPwd = AthStitcher.Security.PasswordHasher.GenerateRandomPassword(24);
                var (hash, salt) = AthStitcher.Security.PasswordHasher.HashPassword(tempPwd);
                admin = new AthStitcher.Data.User
                {
                    Username = "admin",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    ForcePasswordChange = true
                };
                ctx.Users.Add(admin);
                ctx.SaveChanges();
                try { System.Windows.Clipboard.SetText(tempPwd); } catch { }
                MessageBox.Show($"Admin user created.\n\nUsername: admin\nTemporary Password (copied to clipboard):\n{tempPwd}\n\nYou will be asked to change it on first login.",
                    "Admin Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EnforceForcePasswordChangeIfNeeded()
        {
            using var ctx = new AthStitcher.Data.AthStitcherDbContext();
            var admin = ctx.Users.SingleOrDefault(u => u.Username == "admin");
            if (admin != null && admin.ForcePasswordChange)
            {
                if (this.IsLoaded)
                    ChangePasswordForUser("admin", requireCurrent: false);
                else
                    this.Loaded += (_, __) => ChangePasswordForUser("admin", requireCurrent: false);
            }
        }

        private void ChangePasswordForUser(string username, bool requireCurrent)
        {
            var dlg = new ChangePasswordDialog { Username = username };
            if (this.IsLoaded && this.IsVisible) dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;

            using var ctx = new AthStitcher.Data.AthStitcherDbContext();
            var user = ctx.Users.SingleOrDefault(u => u.Username == username);
            if (user == null)
            {
                MessageBox.Show($"User '{username}' not found.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requireCurrent)
            {
                if (!AthStitcher.Security.PasswordHasher.Verify(dlg.Current, user.PasswordHash, user.PasswordSalt))
                {
                    MessageBox.Show("Current password is incorrect.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var (hash, salt) = AthStitcher.Security.PasswordHasher.HashPassword(dlg.NewPwd);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.ForcePasswordChange = false;
            ctx.SaveChanges();
            MessageBox.Show("Password changed successfully.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Parse clipboard time text into seconds (supports ss.fff, m:ss.fff, h:mm:ss.fff)
        private static bool TryParseTimeToSeconds(string text, out double seconds)
        {
            text = (text ?? string.Empty).Trim();
            seconds = 0;
            if (string.IsNullOrEmpty(text)) return false;

            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds) ||
                double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out seconds))
            {
                return true;
            }

            var parts = text.Split(':');
            if (parts.Length >= 2)
            {
                double total = 0;
                if (!double.TryParse(parts[^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double sec) &&
                    !double.TryParse(parts[^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out sec))
                    return false;
                total += sec;
                int multiplier = 60;
                for (int i = parts.Length - 2; i >= 0; i--)
                {
                    if (!int.TryParse(parts[i], out int unit)) return false;
                    total += unit * multiplier;
                    multiplier *= 60;
                }
                seconds = total;
                return true;
            }
            return false;
        }

        // Handle paste-on-click into Results DataGrid Result column (DisplayIndex = 1)
        private void ResultsGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only treat as a special paste if a new result is available from the image click
            if (!_hasNewResultAvailable)
                return;

            // Find DataGridCell from visual tree
            DependencyObject? dep = e.OriginalSource as DependencyObject;
            while (dep != null && dep is not DataGridCell)
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep is not DataGridCell cell)
                return;

            // Only for the Result column
            if (cell.Column?.DisplayIndex != 1)
                return;

            // Get row item
            if (cell.DataContext is not LaneResult lr)
                return;

            // Read clipboard
            string clip;
            try
            {
                if (!Clipboard.ContainsText()) return;
                clip = Clipboard.GetText();
            }
            catch
            {
                return;
            }
            if (!TryParseTimeToSeconds(clip, out double newSeconds)) return;

            // If existing non-zero, confirm overwrite
            double? old = lr.ResultSeconds;
            if ((old != null) && (old != 0.0))
            {
                var res = MessageBox.Show($"Overwrite existing result?\nOld: {old:0.000}\nNew: {newSeconds:0.000}",
                    "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res != MessageBoxResult.Yes) return;
            }

            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                using var ctx = new AthStitcherDbContext();
                // If we previously pasted into a different lane within the same result cycle, clear it
                if (_currentResultLaneIndex.HasValue && _currentResultLaneIndex.Value != lr.Lane)
                {

                    var prev = vm.Results.FirstOrDefault(x => x.Lane == _currentResultLaneIndex.Value && x.HeatId == vm.CurrentHeat.Id);

                    var prevDb = ctx.Results.FirstOrDefault(x => x.Lane == _currentResultLaneIndex.Value && x.HeatId == vm.CurrentHeat.Id);
                    prevDb.ResultSeconds = null; // clears (ResultStr will render blank)
                    ctx.Update(prevDb);
                    ctx.SaveChanges();
                }


                // Set new value and record lane for this cycle
                lr.ResultSeconds = newSeconds;
                ctx.Update(lr);
                ctx.SaveChanges();
                _currentResultLaneIndex = lr.Lane;
                
                // keep _hasNewResultAvailable = true to allow moving the result between lanes in this cycle
                // let focus/edit continue
            }
        }

        // Menu handler to invoke change password (wire from XAML MenuItem)
        private void ChangePassword_Menu_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordForUser("admin", requireCurrent: true);
        }

        // Menu handler to reset admin password (one-time recovery)
        private void ResetAdminPassword_Menu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var conn = Db.CreateConnection();
                var repo = new UserRepository();
                var user = repo.GetByUsername(conn, "admin");
                if (user == null)
                {
                    MessageBox.Show("Admin user not found.", "Reset Password", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string tempPwd = AthStitcher.Security.PasswordHasher.GenerateRandomPassword(24);
                if (repo.ResetPassword(conn, user.Id, tempPwd, forceChange: true))
                {
                    try { System.Windows.Clipboard.SetText(tempPwd); } catch { }
                    MessageBox.Show($"Admin password reset.\n\nTemporary Password (copied to clipboard):\n{tempPwd}\n\nYou'll be asked to change it on next login.",
                        "Reset Password", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to reset admin password.", "Reset Password", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting password: {ex.Message}", "Reset Password", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (TimeLabel != null)
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

        /// <summary>
        /// Attach a unified click handler to Result1..Result9 textboxes.
        /// On click: get clipboard text, round to 3 decimals, paste into the clicked box.
        /// </summary>
        private void WireResultClickPasteHandlers()
        {
            for (int i = 1; i <= 9; i++)
            {
                var tb = this.FindName($"Result{i}") as TextBox;
                if (tb != null)
                {
                    // Avoid multiple subscriptions if constructor gets re-entered (defensive)
                    tb.PreviewMouseLeftButtonDown -= ResultBox_PreviewMouseLeftButtonDown;
                    tb.PreviewMouseLeftButtonDown += ResultBox_PreviewMouseLeftButtonDown;
                }
            }
        }

        private void ResultBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            // Only allow paste if a fresh result (from image click) is available
            if (!_hasNewResultAvailable)
                return;

            string clip = string.Empty;
            try
            {
                if (Clipboard.ContainsText())
                    clip = Clipboard.GetText()?.Trim() ?? string.Empty;
            }
            catch
            {
                // Clipboard can throw in some contexts; ignore and bail
                return;
            }

            if (string.IsNullOrWhiteSpace(clip))
                return;

            if (TryFormatRoundedTo3Decimals(clip, out string formatted))
            {
                // Determine current lane index from TextBox name (expects "ResultN")
                int currentLane = ExtractLaneIndex(tb.Name);

                // If this cell already has a different value, prompt to confirm replacement
                string existing = tb.Text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(existing) && !string.Equals(existing, formatted, StringComparison.Ordinal))
                {
                    var result = MessageBox.Show(
                        $"Replace existing value '{existing}' with '{formatted}'?",
                        "Confirm Replace",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                    {
                        e.Handled = true; // consume click without changing text
                        return;
                    }
                }

                // For the current result cycle: if moving to a different lane, clear previous cell
                if (_currentResultLaneIndex.HasValue && _currentResultLaneIndex.Value != currentLane)
                {
                    var prevTb = this.FindName($"Result{_currentResultLaneIndex.Value}") as TextBox;
                    if (prevTb != null && !ReferenceEquals(prevTb, tb))
                    {
                        prevTb.Clear();
                    }
                }

                // Set new value
                tb.Text = formatted;

                // Update first-paste flag and lane indexes
                if (!_hasPastedAnyResult)
                {
                    _hasPastedAnyResult = true;
                    // First paste hook: add any one-time behavior here if needed
                }
                if (currentLane > 0)
                {
                    _currentResultLaneIndex = currentLane; // track within this result cycle
                    _lastResultLaneIndex = currentLane;
                }
                e.Handled = true; // Prevent default text selection behavior
            }
        }

        private static int ExtractLaneIndex(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            const string prefix = "Result";
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return -1;
            var rest = name.Substring(prefix.Length);
            if (int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
                return idx;
            return -1;
        }

        private static bool TryFormatRoundedTo3Decimals(string input, out string formatted)
        {
            formatted = string.Empty;

            // Try plain number (seconds)
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ||
                double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out d))
            {
                double r = Math.Round(d, 3, MidpointRounding.AwayFromZero);
                formatted = r.ToString("0.000", CultureInfo.InvariantCulture);
                return true;
            }

            // Try time format mm:ss[.fff]
            // Accept forms like m:ss, mm:ss, m:ss.f, m:ss.ff, m:ss.fff
            var parts = input.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
            {
                string secPart = parts[1];
                if (double.TryParse(secPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) ||
                    double.TryParse(secPart, NumberStyles.Float, CultureInfo.CurrentCulture, out seconds))
                {
                    double totalSeconds = minutes * 60 + seconds;
                    totalSeconds = Math.Round(totalSeconds, 3, MidpointRounding.AwayFromZero);
                    int mm = (int)(totalSeconds / 60);
                    double ss = totalSeconds - mm * 60;
                    formatted = string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:00.000}", mm, ss);
                    return true;
                }
            }

            return false;
        }

        bool SkipMetaCheck = false;
        private void StitchButton_Click(object sender, RoutedEventArgs e)
        {

            // If using Stitch Button then reset some properties
            athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false); // Reset the flag for manual or wall clock mode 
            var vidStart = athStitcherViewModel.GetVideoCreationDate();
            athStitcherViewModel.SetEventWallClockStart(vidStart); // Reset the flag for manual or wall clock mode
            var Mode = athStitcherViewModel.GetTimeFromMode();
            //If press Stitch button and WallClaock mode then allow to check for embedded WallClock gun time.
            if (Mode != TimeFromMode.WallClockSelect)
                SkipMetaCheck = true;
            StitchVideo();
        }

        private void StitchVideo()
        {
            TimeFromMode timeFromMode = TimeFromMode.FromVideoStart;
            string outputPath = "";
            int axisHeight = (int)AxisHeightSlider.Value;
            int audioHeight = (int)AudioHeightSlider.Value;
            int threshold = 1000;
            if (athStitcherViewModel.VideoInfo != null)
            {
                outputPath = athStitcherViewModel.GetOutputPath();
                timeFromMode = athStitcherViewModel.VideoInfo.TimeFrom;
            }
            else
            {
                //Default for WallClock Mode
                DateTime? creationDate = DetectAudioFlash.FFMpegActions.GetVideoStart(athStitcherViewModel.GetVideoPath());
                if (creationDate != null)
                {
                    athStitcherViewModel.SetVideoCreationDate(creationDate);
                }
                DateTime videoCreationDate = athStitcherViewModel.GetVideoCreationDate();
                athStitcherViewModel.SetEventWallClockStartTime(videoCreationDate);


                if (!SkipMetaCheck)
                {
                    var videoFn = athStitcherViewModel.GetVideoPath();
                    if (string.IsNullOrEmpty(videoFn) || !File.Exists(videoFn))
                    {
                        MessageBox.Show("Please select a valid video file before stitching.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (Regex.IsMatch(videoFn, @"_[a-zA-Z]+_", RegexOptions.IgnoreCase))
                    {   // If the video filename contains a gun time or start time

                        // Previous version had WallClock guntime embedded in filename
                        // Parse for it and set it the file's meta data.
                        // Then remove it from the filename.
                        if (videoFn.Contains("_WALLCLOCK_", StringComparison.OrdinalIgnoreCase))
                        {
                            string pattern = @"_WALLCLOCK_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})_\.mp4$";

                            Match match = Regex.Match(videoFn, pattern);
                            if (match.Success)
                            {
                                string gunTimeString = match.Groups[1].Value;

                                // Normalize by replacing "--" with ":" in time portion
                                int timeStartIndex = gunTimeString.IndexOf(' ') + 1;
                                string normalized = gunTimeString.Substring(0, timeStartIndex) +
                                                    gunTimeString.Substring(timeStartIndex).Replace("--", ":");

                                DateTime gunDateTime = DateTime.ParseExact(normalized, "yyyy-MM-dd HH:mm:ss.fff", null);
                                PngMetadataHelper.SetMetaInfo(videoFn, "GUNWC", $"GunTime:{gunDateTime:yyyy-MM-dd HH:mm:ss.fff}").GetAwaiter().GetResult();
                            }
                            //Was _GUN_ removal:
                            var videoFn2 = videoFn.Substring(0, videoFn.IndexOf("_WALLCLOCK_", StringComparison.OrdinalIgnoreCase)) + "N.mp4";
                            if (File.Exists(videoFn2))
                            {
                                // Delete the original video file with _Start_ suffix
                                try
                                {
                                    File.Delete(videoFn2);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Delete failed: {ex.Message}");
                                }

                            }
                            File.Copy(videoFn, videoFn2); // Rename the video file to remove the _Start_ suffix
                            athStitcherViewModel.SetVideoPath(videoFn2);
                        }
                        else if (videoFn.Contains("_MANUAL_", StringComparison.OrdinalIgnoreCase))
                        {
                            PngMetadataHelper.SetMetaInfo(videoFn, "MANUAL", $"").GetAwaiter().GetResult();
                            var videoFn2 = videoFn.Substring(0, videoFn.IndexOf("_MANUAL_", StringComparison.OrdinalIgnoreCase)) + "N.mp4";
                            if (File.Exists(videoFn2))
                            {
                                // Delete the original video file with _Start_ suffix
                                try
                                {
                                    File.Delete(videoFn2);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Delete failed: {ex.Message}");
                                }
                            }
                            File.Copy(videoFn, videoFn2); // Rename the video file to remove the _Start_ suffix
                            athStitcherViewModel.SetVideoPath(videoFn2);
                        }
                        else if (videoFn.Contains("_GUNFLASH_", StringComparison.OrdinalIgnoreCase))
                        {
                            PngMetadataHelper.SetMetaInfo(videoFn, "GUNFLASH", $"").GetAwaiter().GetResult();
                            string videoFn2 = videoFn.Substring(0, videoFn.IndexOf("_GUNFLASH_", StringComparison.OrdinalIgnoreCase)) + "N.mp4";
                            if (File.Exists(videoFn2))
                            {
                                // Delete the original video file with _Start_ suffix
                                try
                                {
                                    File.Delete(videoFn2);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Delete failed: {ex.Message}");
                                }

                            }
                            File.Copy(videoFn, videoFn2); // Rename the video file to remove the _Start_ suffix
                            athStitcherViewModel.SetVideoPath(videoFn2);
                        }


                    }

                    // Check for embedded Gun Time
                    var xx = athStitcherViewModel.GetVideoPath();
                    var wt2 = PngMetadataHelper.GetMetaInfo(athStitcherViewModel.GetVideoPath()).GetAwaiter();
                    while (!wt2.IsCompleted)
                    {
                        Thread.Sleep(1000); // Wait for the metadata to be retrieved
                    }
                    var metaInfo = wt2.GetResult(); // Wait for the metadata to be retrieved
                    if (metaInfo != null)
                    {
                        if (metaInfo.Item1.Contains("WALLCLOCK", StringComparison.OrdinalIgnoreCase))
                        {
                            string dt = metaInfo.Item2;
                            if (string.IsNullOrEmpty(dt))
                            {
                                if (DateTime.TryParse(dt, out DateTime dat))
                                {
                                    athStitcherViewModel.SetEventWallClockStart(dat);
                                    athStitcherViewModel.SetTimeFromMode(TimeFromMode.WallClockSelect);
                                }
                            }
                        }
                        else if (metaInfo.Item1.Contains("MANUAL", StringComparison.OrdinalIgnoreCase))
                        {
                            athStitcherViewModel.SetTimeFromMode(TimeFromMode.ManuallySelect);
                        }
                        else if (metaInfo.Item1.Contains("VIDEOSTART", StringComparison.OrdinalIgnoreCase))
                        {
                            athStitcherViewModel.SetTimeFromMode(TimeFromMode.FromVideoStart);
                            athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false); // Explicitly set for FromVideoStart mode
                        }
                        else if (metaInfo.Item1.Contains("GUNSOUND", StringComparison.OrdinalIgnoreCase))
                        {
                            athStitcherViewModel.SetTimeFromMode(TimeFromMode.FromGunSound);
                        }
                        else if (metaInfo.Item1.Contains("GUNFLASH", StringComparison.OrdinalIgnoreCase))
                        {
                            athStitcherViewModel.SetTimeFromMode(TimeFromMode.FromGunFlash);
                        }
                        else
                        {
                            PngMetadataHelper.ClearMetaInfo(athStitcherViewModel.GetGunAudioPath()).GetAwaiter().GetResult();
                            MessageBox.Show("Invalid timing metadata found in video. Clearing so please restart.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                            // Default to FromVideoStart if no specific metadata is found
                            //athStitcherViewModel.SetTimeFromMode(TimeFromMode.FromVideoStart);
                            //athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false); // Explicitly set for FromVideoStart mode
                        }
                    }
                }

                // Get info needed by videoStitcher but as properties of the ViewModel can get at its constructor
                string videoFilePath = athStitcherViewModel.GetVideoPath();

                timeFromMode = athStitcherViewModel.GetTimeFromMode();
                if (!File.Exists(videoFilePath))
                {
                    MessageBox.Show("The specified video file does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    StitchButton.IsEnabled = true;
                    return;
                }

                //Another tidyup. Gun info was stored in Image fiilename
                // Remove any _Start_ or _gun_ from the output path
                outputPath = athStitcherViewModel.GetOutputPath();
                while (outputPath.Contains("_start_", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = outputPath.Substring(0, outputPath.IndexOf("_Start_", StringComparison.OrdinalIgnoreCase));
                    outputPath = $"{outputPath}.png";
                }
                while (outputPath.Contains("_gun_", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = outputPath.Substring(0, outputPath.IndexOf("_gun_", StringComparison.OrdinalIgnoreCase));
                    outputPath = $"{outputPath}.png";
                }
                athStitcherViewModel.SetOutputPath(outputPath);
            }

            // Hide all transient UI elements
            PopupVideoFrameImage.IsOpen = false; // Close the popup if it is open
            WatchClockDateTimePopup.IsOpen = false;
            StartVerticalLine.Visibility = Visibility.Collapsed;
            VerticalLine.Visibility = Visibility.Collapsed;
            NudgeVerticalLine.Visibility = Visibility.Collapsed; // Hide the nudge vertical line
            TimeLabel.Visibility = Visibility.Collapsed;
            // Hide the other UI elements (ie Excluding Stitched iamge)
            athStitcherViewModel.SetMyVisibility(Visibility.Collapsed);
            Thread.Yield();


            // Used by manually select mode, later
            // Need to get stitched image first
            // You can then set it.
            athStitcherViewModel.SetSelectedStartTime(0);

            // Show the busy indicator
            BusyIndicator.Visibility = Visibility.Visible;

            // More setup
            DetectVideoFlash.ActionVideoAnalysis? actionVideoAnalysis = null;
            VideoDetectMode videoDetectMode = athStitcherViewModel.GetVideoDetectMode();
            if (timeFromMode == TimeFromMode.FromVideoStart)
            {
                // Nothing 2Do
            }
            else if (timeFromMode == TimeFromMode.FromGunSound)
            {
                //DetectVideoFlash.FFMpegActions.Filterdata(videoFilePath, guninfoFilePath);
            }
            else if (timeFromMode == TimeFromMode.FromGunFlash)
            {
            }
            else if (timeFromMode == TimeFromMode.ManuallySelect)
            {
                var start = athStitcherViewModel.GetEventWallClockStartTime();
            }
            // Validate 
            // Read inputs
            //string gunAudioPath = GunAudioPath();

            athStitcherViewModel.SetOutputPath(outputPath);
            StitchButton.Width = 0;
            StitchButton.IsEnabled = false; // Disable the button to prevent multiple clicks
                                            // Validate inputs
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath); // Delete existing output file if it exists
                //return;
            }

            if (!int.TryParse(Threshold.Text, out int _threshold))
            {
                MessageBox.Show("Please enter a valid number >0  (Typical 1000) for threshold.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                threshold = _threshold;

                return;
            }
            threshold = int.Parse(Threshold.Text);


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

            string gunAudioPath = athStitcherViewModel.GetGunAudioPath();
            videoDetectMode = athStitcherViewModel.GetVideoDetectMode();

            // Run the stitching process in a background thread
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (s, args) =>
            {
                //Determine guntime
                // if (HaveGotGunTime)
                //{
                //HaveGotGunTime = false;
                if (timeFromMode == TimeFromMode.FromVideoStart)
                {
                    //Need next to get video length
                    var xx = videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    GunTimeDbl = 0; // Default value when timing is from button press
                    GunTimeIndex = 0; // Default index when timing is from button press
                    athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false); // Explicitly reset for FromVideoStart mode
                }
                else if (timeFromMode == TimeFromMode.FromGunSound)
                {
                    GunTimeDbl = videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    GunTimeIndex = videoStitcher.GunTimeIndex;
                }
                else if (timeFromMode == TimeFromMode.FromGunFlash)
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
                    athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false); // Explicitly reset for ManuallySelect mode
                }
                else if (timeFromMode == TimeFromMode.WallClockSelect)
                {
                    DateTime videoCreationDate = athStitcherViewModel.GetVideoCreationDate();
                    DateTime gunDateTime = athStitcherViewModel.GetEventWallClockStartTime();
                    TimeSpan GunTime = gunDateTime.Subtract(videoCreationDate);
                    //Need next to get video length
                    //GunTimeDbl = 0.7;// videoStitcher.GetGunTimenFrameIndex(gunAudioPath);
                    double gunTimSec = (GunTime.TotalMilliseconds) / 1000;
                    GunTimeDbl = videoStitcher.GetGunTimenFrameIndex($"{gunTimSec}");
                    GunTimeIndex = videoStitcher.GunTimeIndex;
                    athStitcherViewModel.SetGunTime(GunTimeDbl, GunTimeIndex);
                    athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                }
                //}
                //else
                //{
                //    //GunTimeDbl = 0; // Default value when timing is from button press
                //    //GunTimeIndex = 0; // Default index when timing is from button press

                //}


                videoStitcher.Stitch(athStitcherViewModel.GetExifToolFolder());
                // Add metadata to the stitched image

            };

            worker.RunWorkerCompleted += async (s, args) =>
            {

                //string imagepath =  PngMetadataHelper.AppendGunTimeImageFilename(athStitcherViewModel.GetOutputPath(), GunTimeDbl);
                //string imagepath = athStitcherViewModel.GetOutputPath();
                string videoStart = athStitcherViewModel.GetVideoCreationDateStr();

                await PngMetadataHelper.SetMetaInfo(
                    athStitcherViewModel.GetOutputPath(),
                    $"VideoStart:{videoStart}",
                    $"Guntime:{GunTimeDbl}");

                //Next bit only for debugging
                var metaInfo = await PngMetadataHelper.GetMetaInfo(athStitcherViewModel.GetOutputPath()); // Wait for the metadata to be retrieved
                                                                                                          //AddMetadataToPng(@"C:\temp\vid\cars\notwo.png", @"C:\temp\vid\cars\notwocpy.png", "XXX", "A TITLE").Wait();


                videoLength = videoStitcher.videoDuration;
                athStitcherViewModel.SetVideoLength(videoLength);
                athStitcherViewModel.SetGunTime(GunTimeDbl, GunTimeIndex); // Set the gun time in the ViewModel

                //athStitcherViewModel.SetVideoLength(videoLength);





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
                    //.. that is its is DateTime.MinValue
                    // set it to the video creation date
                    if (athStitcherViewModel.GetEventWallClockStartTime() == DateTime.MinValue)
                    {
                        athStitcherViewModel.SetEventWallClockStartTime(athStitcherViewModel.GetVideoCreationDate());
                    }

                }
                switch (athStitcherViewModel.GetTimeFromMode())
                {
                    case TimeFromMode.ManuallySelect:
                        HaveGotGunTime = false;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                        break;
                    case TimeFromMode.WallClockSelect:
                        HaveGotGunTime = true;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                        break;
                    case TimeFromMode.FromVideoStart:
                        HaveGotGunTime = true;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                        break;
                    case TimeFromMode.FromGunFlash:
                        HaveGotGunTime = false;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                        break;
                    case TimeFromMode.FromGunSound:
                        HaveGotGunTime = false;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                        break;

                }
                this.DataContext = null;
                this.DataContext = athStitcherViewModel.DataContext;
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
                // Only process right button if image is stitched, in Manual mode and guntime not yet set
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

        private void StitchedImage_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
            // A new result is now available to paste once into a lane
            _hasNewResultAvailable = true;
            _currentResultLaneIndex = null; // reset placement for new result cycle
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
                    athStitcherViewModel.SetGunTime(timeFromGunStart, frameNo);
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
                    // Default to the folder selected in GetVideoPage if available
                    if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                        && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                    {
                        openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    }
                }
            }
            else
            {
                openFileDialog = new OpenFileDialog
                {
                    Filter = "MP4 Files (*.mp4)|*.mp4",
                };
                // Default to the folder selected in GetVideoPage if available
                if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                    && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                {
                    openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                }
            }

            // Prefer the folder selected in GetVideoPage, if available
            if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
            {
                openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                // Clear FileName so InitialDirectory takes effect
                openFileDialog.FileName = string.Empty;
            }

            var originalCwd = Environment.CurrentDirectory;
            try
            {
                if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                    && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                {
                    Environment.CurrentDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    openFileDialog.FileName = string.Empty;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    videoFilePath = openFileDialog.FileName;
                    StitchVideo(videoFilePath);
                }
            }
            finally
            {
                Environment.CurrentDirectory = originalCwd;
            }
        }

        public void StitchVideo(string videoFilePath)
        {

            athStitcherViewModel.SetVideoPath(videoFilePath);
            // Two-way: update shared folder from selected video file
            var selDir = System.IO.Path.GetDirectoryName(videoFilePath);
            if (!string.IsNullOrEmpty(selDir) && Directory.Exists(selDir))
            {
                AthStitcherGUI.SharedAppState.SetGlobalFolder(selDir);
            }

            string imagePath = Regex.Replace(videoFilePath, ".mp4", ".png", RegexOptions.IgnoreCase);
            athStitcherViewModel.SetGunTime(0, 0);
            HaveGotGunTime = false; // Reset the gun time flag

            string jsonFilePath = Regex.Replace(videoFilePath, ".mp4", ".json", RegexOptions.IgnoreCase);
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                VideoInfo videoInfo = VideoInfo.CreateFromJson(json);
                ////videoFilePath = openFileDialog.FileName;
                athStitcherViewModel.VideoInfo = videoInfo;
                // Store the VideoInfo object in the ViewModel
                athStitcherViewModel.SetTimeFromMode(videoInfo.TimeFrom);
                athStitcherViewModel.SetVideoCreationDate(videoInfo.VideoStart);
                // If GunTime is not set (MinValue), fall back to VideoStart
                // Guarantee non-null by defaulting to DateTime.MinValue
                DateTime? eventStartCandidate = (videoInfo.GunTime == DateTime.MinValue)
                    ? videoInfo.VideoStart
                    : videoInfo.GunTime;
                var eventStart = eventStartCandidate ?? DateTime.MinValue;
                athStitcherViewModel.SetEventWallClockStart(eventStart);
                HaveGotGunTime = false;
                switch (videoInfo.TimeFrom)
                {
                    case TimeFromMode.ManuallySelect:
                        HaveGotGunTime = false;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                        break;
                    case TimeFromMode.WallClockSelect:
                        HaveGotGunTime = true;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                        break;
                    case TimeFromMode.FromVideoStart:
                        HaveGotGunTime = true;
                        // Ensure a non-nullable DateTime is passed
                        var vs = (videoInfo.VideoStart.HasValue && videoInfo.VideoStart.Value != DateTime.MinValue)
                            ? videoInfo.VideoStart.Value
                            : DateTime.MinValue;
                        athStitcherViewModel.SetEventWallClockStart(vs);
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                        break;
                    case TimeFromMode.FromGunFlash:
                        HaveGotGunTime = false;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                        break;
                    case TimeFromMode.FromGunSound:
                        HaveGotGunTime = false;
                        athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                        break;

                }
            }
            else
            {
                HaveGotGunTime = false;
                // No match or json found, use what was selected on the Menu
                Console.WriteLine($"No match found.");

                imagePath = Regex.Replace(videoFilePath, ".mp4", ".png", RegexOptions.IgnoreCase);
                DateTime videoCreationDate = athStitcherViewModel.GetVideoCreationDate();
                // Set the mode to ManuallySelect
                athStitcherViewModel.SetTimeFromMode(TimeFromMode.ManuallySelect);
                athStitcherViewModel.SetVideoCreationDate(DateTime.MinValue);
                athStitcherViewModel.SetEventWallClockStart(DateTime.MinValue);//GunTime Wallclock
                VideoInfo videoInfo = new VideoInfo();
                videoInfo.TimeFrom = TimeFromMode.ManuallySelect;
                videoInfo.VideoStart = DateTime.MinValue;
                videoInfo.GunTime = DateTime.MinValue;
                athStitcherViewModel.VideoInfo = videoInfo;
                athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false); // Explicitly reset for new ManuallySelect mode

            }

            // Store the VideoInfo object in the ViewModel
            athStitcherViewModel.SetOutputPath(imagePath);
            SkipMetaCheck = false;
            StitchVideo();
            // Explicitly set the checkbox state based on the TimeFromMode
            TimeFromMode currentMode = athStitcherViewModel.GetTimeFromMode();
            if (currentMode == TimeFromMode.WallClockSelect || currentMode == TimeFromMode.FromVideoStart)
            {
                viewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
            }
            else if (HaveGotGunTime)
            {
                viewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
            }
            else
            {
                viewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
            }
            if (athStitcherViewModel.GetTimeFromMode() == TimeFromMode.WallClockSelect)
            {
                //Ok_Click(this, e);
            }

        }


        /// <summary>
        /// Select the MP4 file but not open it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>DownLoadMp4File_Click
        private void DownLoadMp4File_Click(object sender, RoutedEventArgs e)
        {
            ((App)System.Windows.Application.Current).OpenGetVideoPage();
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
                    // Default to the folder selected in GetVideoPage if available
                    if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                        && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                    {
                        openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    }
                }
            }
            else
            {
                openFileDialog = new OpenFileDialog
                {
                    Filter = "PNG Files (*.png)|*.png",
                };
                // Default to the folder selected in GetVideoPage if available
                if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                    && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                {
                    openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                }
            }

            // Prefer the folder selected in GetVideoPage, if available
            if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
            {
                openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                // Clear FileName so InitialDirectory takes effect
                openFileDialog.FileName = string.Empty;
            }

            // Enforce global folder by setting current directory temporarily
            var originalCwdPng = Environment.CurrentDirectory;
            try
            {
                if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                    && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                {
                    Environment.CurrentDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    openFileDialog.FileName = string.Empty;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    OutputFilePath = openFileDialog.FileName;
                    athStitcherViewModel.SetOutputPath(OutputFilePath); // Update the ViewModel with the new path
                    // Two-way: update shared folder from selected PNG file
                    var selDir = System.IO.Path.GetDirectoryName(OutputFilePath);
                    if (!string.IsNullOrEmpty(selDir) && Directory.Exists(selDir))
                    {
                        AthStitcherGUI.SharedAppState.SetGlobalFolder(selDir);
                    }
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
            finally
            {
                Environment.CurrentDirectory = originalCwdPng;
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
                    // Default to the folder selected in GetVideoPage if available
                    if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                        && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                    {
                        openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    }
                }

                // Prefer the folder selected in GetVideoPage, if available
                if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                    && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                {
                    openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    // Clear FileName so InitialDirectory takes effect
                    openFileDialog.FileName = string.Empty;
                }

                // Enforce global folder by setting current directory temporarily
                var originalCwdTxt = Environment.CurrentDirectory;
                try
                {
                    if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                        && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                    {
                        Environment.CurrentDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                        openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                        openFileDialog.FileName = string.Empty;
                    }

                    if (openFileDialog.ShowDialog() == true)
                    {
                        GunAudioPathInput = openFileDialog.FileName;
                        athStitcherViewModel.SetGunAudioPath(GunAudioPathInput); // Update the ViewModel with the new path
                                                                                 // Two-way: update shared folder from selected TXT file
                        var selDir = System.IO.Path.GetDirectoryName(GunAudioPathInput);
                        if (!string.IsNullOrEmpty(selDir) && Directory.Exists(selDir))
                        {
                            AthStitcherGUI.SharedAppState.SetGlobalFolder(selDir);
                        }
                    }
                }
                finally
                {
                    Environment.CurrentDirectory = originalCwdTxt;
                }
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

                // Explicitly set HaveSelectedandShownGunLineToManualorWallClockMode based on the selected mode
                if (timeMode == TimeFromMode.WallClockSelect || timeMode == TimeFromMode.FromVideoStart)
                {
                    athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(true);
                }
                else
                {
                    athStitcherViewModel.Set_HaveSelectedandShownGunLineinManualorWallClockMode(false);
                }
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
            // Just incase ....
            if (FrameImage.Height > ImageCanvas.Height)
            {
                FrameImage.Height = ImageCanvas.Height;
                var width = FrameImage.Source.Width;
                var height = FrameImage.Source.Height;
                double ratio = width / height;
                FrameImage.Width = ratio * FrameImage.Height;
            }
            PopupVideoFrameImage.Width = FrameImage.Width;
            PopupVideoFrameImage.Height = FrameImage.Height;
            PopupVideoFrameImage.HorizontalOffset = FrameImage.Width / 2;
            Divider.Y2 = FrameImage.Height;

            //PopupVideoFrameImage.HorizontalOffset =  (int)(PopupVideoFrameImage.Width / 2); // GetPopupWidth();
            //PopupVideoFrameImage.VerticalOffset = 0;// (int)PopupVideoFrameImage.Height; /*GetTimeLabelMargin().Top + TimeLabel.ActualHeight +115;*/

            VideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
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
            // Just incase ....
            if (NudgeFrameImage.Height > ImageCanvas.Height)
            {
                NudgeFrameImage.Height = ImageCanvas.Height;
                var width = NudgeFrameImage.Source.Width;
                var height = NudgeFrameImage.Source.Height;
                double ratio = width / height;
                NudgeFrameImage.Width = ratio * NudgeFrameImage.Height;
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

                outputPath = PngMetadataHelper.AppendGunTimeImageFilename(outputPath, guntime);
                athStitcherViewModel.SetOutputPath(outputPath);
                if (gunTimeIndex <= 0)
                    gunTimeIndex = videoStitcher.AddGunLine(guntime, athStitcherViewModel.GetGunColor(), outputPath);

                LoadStitchedImage(outputPath);

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
            // Determine if have have a line to nudge, more particular if just clicked or nudged
            var prevline = (StartVerticalLine.Visibility == Visibility.Visible)
            ? StartVerticalLine
            : (VerticalLine.Visibility == Visibility.Visible) ? VerticalLine
            : (NudgeVerticalLine.Visibility == Visibility.Visible) ? NudgeVerticalLine : null;
            if (prevline == null)
                return;

            if (prevline != NudgeVerticalLine)
            {
                // Change to using the Nudge line
                prevline.Visibility = Visibility.Collapsed;
                NudgeVerticalLine.X1 = prevline.X1;
                NudgeVerticalLine.X2 = prevline.X2;
                NudgeVerticalLine.Y1 = prevline.Y1;
                NudgeVerticalLine.Y2 = prevline.Y2;
                NudgeVerticalLine.Visibility = Visibility.Visible;
                horizOffsetz = NudgeVerticalLine.X1;
                NudgePopupVideoFrameImage.Width = 100;
                NudgePopupVideoFrameImage.Height = 100;
                NudgeVideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
            if (sender is Button button)
            {
                string toolTip = button.ToolTip?.ToString() ?? "";
                if (!toolTip.Contains("WC"))
                    Nudge(toolTip);
                else
                    NudgeWC(toolTip);
            }
            ImageCanvas.UpdateLayout();
            NudgeVerticalLine.UpdateLayout();
            horizOffset = NudgeVerticalLine.X1 - horizOffsetz;

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
                if (posX >= 1 * horizontalScale)
                {
                    //Back one Frame
                    posX -= 1 * horizontalScale;
                }
            }
            else if (toolTip == "Forward")
            {
                if (posX <= (stitchedImageVirtualWidth - 1 * horizontalScale))
                {
                    //Forward one Frame
                    posX += 1 * horizontalScale;
                }
            }
            else if (toolTip == "Back 5")
            {
                if (posX >= 5 * horizontalScale)
                {
                    //Back five Frames
                    posX -= 5 * horizontalScale;
                }
            }
            else if (toolTip == "Forward 5")
            {
                if (posX < (stitchedImageVirtualWidth - 5 * horizontalScale))
                {
                    //Forward five Frames
                    posX += 5 * horizontalScale;
                }
            }
            else if (toolTip == "Back 1 sec")
            {
                if (posX >= oneSecNoFrames * horizontalScale)
                {
                    //Back five Frames
                    posX -= oneSecNoFrames * horizontalScale;
                }
            }
            else if (toolTip == "Forward 1 sec")
            {
                if (posX < (stitchedImageVirtualWidth - oneSecNoFrames * horizontalScale))
                {
                    //Forward five Frames
                    posX += oneSecNoFrames * horizontalScale;
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

            horizOffset = posX - horizOffset;
            verticalOffset = posY2; ;
            UpdateTimeLabel(posX, startTime, isLeft);
            if (athStitcherViewModel.GetShowVideoFramePopup())
            {
                if (athStitcherViewModel.GetNudge_useVideoFrameratherthanNudgeFrame())
                {
                    NudgePopupVideoFrameImage.IsOpen = false;
                    DisplayFrame(frameNo, posX, false);
                }
                else
                {
                    if (PopupVideoFrameImage.IsOpen)
                    {
                        //Use same dimensions for popup iamge
                        NudgeFrameImage.Width = FrameImage.Width;
                        NudgeFrameImage.Height = FrameImage.Height;
                        PopupVideoFrameImage.IsOpen = false;
                    }
                    NudgeDisplayFrame(frameNo, posX, false);
                }
            }
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
            TimeSpan oneFrameTs = new TimeSpan(0, 0, 0, 0, 0, (int)Math.Round(1000000 / Fps, 0));

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
                    VideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    if (FrameImage.Width < athStitcherViewModel.GetMinPopupWidth() / 2 || FrameImage.Height < athStitcherViewModel.GetMinPopupHeight() / 2)
                    {
                        PopupVideoFrameImage.IsOpen = false; // Close popup if too small
                    }
                }
                else
                {
                    if (PopupVideoFrameImage.Height * 1.5 < ImageCanvas.Height)
                    {
                        VideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        PopupVideoFrameImage.Width *= 1.5;
                        PopupVideoFrameImage.Height *= 1.5;
                    }
                    else
                    {
                        VideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    }
                    FrameImage.Width *= 1.5; // Close popup
                    FrameImage.Height *= 1.5;
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
                    NudgeVideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    if (NudgeFrameImage.Width < athStitcherViewModel.GetMinPopupWidth() / 2 || NudgeFrameImage.Height < athStitcherViewModel.GetMinPopupHeight() / 2)
                    {
                        double ratio = NudgePopupVideoFrameImage.Width / NudgePopupVideoFrameImage.Height;
                        NudgePopupVideoFrameImage.Height = 100;
                        NudgePopupVideoFrameImage.Width = 100 / ratio;
                        NudgePopupVideoFrameImage.IsOpen = false; // Close popup if too small
                    }
                }
                else
                {
                    if (NudgePopupVideoFrameImage.Height * 1.5 < ImageCanvas.Height)
                    {
                        NudgePopupVideoFrameImage.Width *= 1.5;
                        NudgePopupVideoFrameImage.Height *= 1.5;
                        NudgeVideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    }
                    else
                    {
                        NudgeVideoFrameScrollbar.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    }
                    NudgeFrameImage.Width *= 1.5; // Close popup
                    NudgeFrameImage.Height *= 1.5;
                }
            }
            else if (e.ClickCount == 2) // Detect double-click
            {
                double ratio = NudgePopupVideoFrameImage.Width / NudgePopupVideoFrameImage.Height;
                NudgePopupVideoFrameImage.Height = 100;
                NudgePopupVideoFrameImage.Width = 100 / ratio;
                NudgePopupVideoFrameImage.IsOpen = false; // Close popup
            }
        }

        private void ImageKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
            if (NudgeVerticalLine.Visibility == Visibility.Collapsed)
            {
                if (StartVerticalLine.Visibility == Visibility.Visible)
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

                if (DataContext is ViewModels.AthStitcherModel viewModel)
                {

                    // Using previous VideoFrame context instead
                    if (athStitcherViewModel.GetNudge_useVideoFrameratherthanNudgeFrame())
                        return;
                }

                if (NudgePopupVideoFrameImage.Width < athStitcherViewModel.GetMinPopupWidth() / 2)
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

                System.Windows.Point fakeMousePoint = new System.Windows.Point(horizOffset + 100 + athStitcherViewModel.GetGunTime(), 0); // arbitrarily chosen coordinates
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
                            NudgePopupVideoFrameImage.HorizontalOffset = -ratio * NudgePopupVideoFrameImage.Width;
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
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MP4 Files (*.mp4)|*.mp4",
            };
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
                    // Default to the folder selected in GetVideoPage if available
                    if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                        && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                    {
                        openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    }
                }
            }


            // Enforce global folder by setting current directory temporarily
            var originalCwdMp4b = Environment.CurrentDirectory;
            try
            {
                if (!string.IsNullOrEmpty(AthStitcherGUI.SharedAppState.GlobalFolder)
                    && Directory.Exists(AthStitcherGUI.SharedAppState.GlobalFolder))
                {
                    Environment.CurrentDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    openFileDialog.InitialDirectory = AthStitcherGUI.SharedAppState.GlobalFolder;
                    openFileDialog.FileName = string.Empty;
                }

                if (openFileDialog.ShowDialog() == true)
                {
                    videoFilePath = openFileDialog.FileName;
                    athStitcherViewModel.SetVideoPath(videoFilePath);
                    // Two-way: update shared folder from selected MP4 file
                    var selDir = System.IO.Path.GetDirectoryName(videoFilePath);
                    if (!string.IsNullOrEmpty(selDir) && Directory.Exists(selDir))
                    {
                        AthStitcherGUI.SharedAppState.SetGlobalFolder(selDir);
                    }
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
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to select video file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Environment.CurrentDirectory = originalCwdMp4b;
            }
        }

        private void QRCode_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://davidjones.sportronics.com.au/appdev/Photo_Finish-Video_Capture_and_Processing-appdev.html";
            MessageBox.Show($"Documentation URL:\n{url}", "Photo Finish Documentation", MessageBoxButton.OK, MessageBoxImage.Information);

            // Open the URL in the default browser
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditMetaInfo_Click(object sender, RoutedEventArgs e)
        {
            var editor = new JsonEditorWindow();
            editor.ShowDialog();
        }

        private void GetExifTool(object sender, RoutedEventArgs e)
        {
            // @"C:\temp\vid\exiftool-13.32_64\exiftool-13.32_64";//Need to download from https://exiftool.org/
            string url = "https://exiftool.org/";
            //MessageBox.Show($"Get exifTool URL:\n{url}", "Required Photo Finish Documentation", MessageBoxButton.OK, MessageBoxImage.Information);
            var res = MessageBox.Show(
                $"Get exifTool URL:\n{url}",
                "Downl",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information
            );
            if (res != MessageBoxResult.OK)
                return;
            // Open the URL in the default browser
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void UnzipExifTool(object sender, RoutedEventArgs e)
        {
            // Step 1: Select the ZIP file
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ZIP Files (*.zip)|*.zip",
                Title = "Select the downloaded ZIP file"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                MessageBox.Show($"No file selected", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(openFileDialog.FileName) || !File.Exists(openFileDialog.FileName))
            {
                MessageBox.Show($"Selected file does not exist (or blank):\n{openFileDialog.FileName}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            string zipPath = openFileDialog.FileName;
            string zipFileName = System.IO.Path.GetFileName(zipPath);
            string zipFileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(zipFileName);
            var ExifToolFolder = athStitcherViewModel.DataContext.ExifToolFolder;
            // If ExifToolFolder is not set or invalid, default to the directory of the ZIP file
            if (string.IsNullOrWhiteSpace(ExifToolFolder) || !File.Exists(ExifToolFolder))
            {
                ExifToolFolder = System.IO.Path.GetDirectoryName(zipPath);
            }

            // Step 2: Select the destination folder
            var folderDialog = new OpenFolderDialog
            {
                Title = "Select the folder to extract the ZIP contents to",
                InitialDirectory = ExifToolFolder // Set initial folder
            };

            // Show the dialog
            if (!folderDialog.ShowDialog() != true) return;

            if (string.IsNullOrWhiteSpace(folderDialog.FolderName))
                return;

            string extractPath = folderDialog.FolderName;

            // Step 3: Extract the ZIP file
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                MessageBox.Show($"ZIP extracted to:\n{extractPath}", "Extraction Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                var ExifTool = athStitcherViewModel.DataContext.ExifTool;
                var ExifToolExe = athStitcherViewModel.DataContext.ExifToolExe;
                ////ExifToolFolder = athStitcherViewModel.DataContext.ExifToolFolder;
                if (zipFileNameWithoutExt.StartsWith(ExifTool))
                {
                    // Check if exiftool(-k).exe exists in the extracted folder one or two deep
                    string zipfilename = System.IO.Path.GetFileNameWithoutExtension(zipPath);
                    string folder = Regex.Replace(zipfilename, @" \(\d+\)$", "");

                    string extractedPath = System.IO.Path.Combine(extractPath, folder);
                    string filePath = System.IO.Path.Combine(extractedPath, ExifToolExe);

                    if (File.Exists(filePath))
                    {
                        ExifToolFolder = extractedPath;
                    }
                    else
                    {
                        string extracted2Path = System.IO.Path.Combine(extractedPath, folder);
                        string filePath2 = System.IO.Path.Combine(extracted2Path, $"{ExifToolExe}");

                        if (File.Exists(filePath2))
                        {
                            ExifToolFolder = $"{extracted2Path}";
                        }
                        else
                        {
                            MessageBox.Show($"Could not find {ExifToolExe} in the extracted folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    };
                    athStitcherViewModel.SetExifToolFolder(ExifToolFolder);
                    athStitcherViewModel.SaveViewModel();
                    MessageBox.Show($"ExifTool folder set to:\n{ExifToolFolder}", "ExifTool Location", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to get/extract ZIP:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetExifToolLocation(object sender, RoutedEventArgs e)
        {
            var ExifTool = athStitcherViewModel.DataContext.ExifTool;
            var ExifToolExe = athStitcherViewModel.DataContext.ExifToolExe;
            var ExifToolFolder = athStitcherViewModel.DataContext.ExifToolFolder;

            // Use Windows Forms FolderBrowserDialog for folder selection
            var dialog = new OpenFolderDialog
            {
                Title = "Select the folder containing exiftool.exe",
                InitialDirectory = ExifToolFolder // Set initial folder
            };

            // Show the dialog
            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrWhiteSpace(dialog.FolderName))
                {
                    string extractedPath = dialog.FolderName;

                    string filePath = System.IO.Path.Combine(extractedPath, ExifToolExe);
                    if (File.Exists(filePath))
                    {
                        ExifToolFolder = extractedPath;
                    }
                    else
                    {
                        MessageBox.Show($"Could not find {ExifToolExe} in the selected folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    ;

                    // You can store this path in your ViewModel, settings, or use it directly
                    MessageBox.Show($"ExifTool folder set to:\n{ExifToolFolder}", "ExifTool Location", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Example: Save to ViewModel (add a property if needed)
                    athStitcherViewModel.SetExifToolFolder(ExifToolFolder);
                    athStitcherViewModel.SaveViewModel();
                }
            }
        }


        private void AboutExifTool(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Needed for storing meta-info with stitched iamge.", $"About XifTool", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AboutGunTimeLineColor(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Can set the color of the drawn line for the race start.", $"About Gun Time Line Color", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetExifToolName(object sender, RoutedEventArgs e)
        {
            string currentName = athStitcherViewModel.DataContext.ExifTool;
            var dialog = new ExifToolNamingDialog(currentName) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                string newName = dialog.InputText.Trim();
                if (!string.IsNullOrEmpty(newName) && newName != currentName)
                {
                    athStitcherViewModel.DataContext.ExifTool = newName;
                    athStitcherViewModel.SaveViewModel();
                    MessageBox.Show($"ExifTool name changed to: {newName}", "ExifTool Name", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                string newExeName = dialog.InputText2.Trim();
                if (!string.IsNullOrEmpty(newExeName) && newExeName != currentName)
                {
                    newExeName = $"{newExeName}.exe";
                    athStitcherViewModel.DataContext.ExifToolExe = newExeName;
                    athStitcherViewModel.SaveViewModel();
                    MessageBox.Show($"ExifToolAppName name changed to: {newExeName}", "ExifTool App Name", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Hide_Sliders_Button_Click(object sender, RoutedEventArgs e)
        {
            athStitcherViewModel.SetShowSliders(false);
        }

        private void Show_Sliders_Button_Click(object sender, RoutedEventArgs e)
        {
            athStitcherViewModel.SetShowSliders(true);
        }

        #region Meets,Events,Heats Management
        private void Select_Meet_Menu_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AthStitcher.Views.SelectMeetDialog { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true && dlg.SelectedMeet != null)
            {
                // Set current meet object on ViewModel for XAML bindings
                if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
                {
                    vm.CurrentMeet = dlg.SelectedMeet;
                    athStitcherViewModel.SetShowSliders(false);
                }
            }
        }
        private void Manage_Meets_Menu_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AthStitcher.Views.ManageMeetsDialog { Owner = this };
            dlg.vm = this.DataContext as AthStitcherGUI.ViewModels.AthStitcherModel;
            var result = dlg.ShowDialog();
            if (result == true && dlg.SelectedMeet != null)
            {
                // Set current meet object on ViewModel for XAML bindings
                if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
                {
                    vm.CurrentMeet = dlg.SelectedMeet;
                    athStitcherViewModel.SetShowSliders(false);
                }
            }
        }

        private void New_Meet_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            DateTime meetCutoff = DateTime.Now.Date;

            int cutoff = vm.Scheduling?.MeetCutoff ?? 0;
            meetCutoff = DateTime.Now.AddDays(cutoff);

            Meet meet = new Meet
            {
                Description = "<Enter Meet description>",
                Date = meetCutoff,
                Location = "<Enter Meet Location>",
                Round = 1

            };
            var dlg = new NewMeetDialog { Owner = this };
            dlg.Meet = meet;
            dlg.CutOff = meetCutoff;
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                meet = dlg.Meet;
                string desc = meet.Description;
                if (string.IsNullOrWhiteSpace(desc))
                {
                    MessageBox.Show("Description cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                int? round = meet.Round; //Not mandatory so no checks except >0
                if ((round != null) && (round <= 0))
                {
                    MessageBox.Show("Round must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var date = meet.Date;
                if ((date == null) || (date < meetCutoff))
                {
                    MessageBox.Show("Date before cut-off date {meetCutoff}.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var loc = meet.Location;
                if (string.IsNullOrWhiteSpace(loc))
                {
                    MessageBox.Show("Location cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Duplicate check: same Description + Date
                bool exists = ctx.Meets.Any(m => m.Description == desc && m.Date == date && m.Location == loc);
                if (exists)
                {
                    MessageBox.Show("A meet with the same Description and Date already exists.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                //var meet = new Meet { Description = desc, Date = date, Location = loc };
                ctx.Meets.Add(meet);
                ctx.SaveChanges();
                vm.CurrentMeet = meet;
            }
        }

        private void Manage_Events_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;

            // Ensure a meet is selected first
            if (vm.CurrentMeet == null)
            {
                var pick = new AthStitcher.Views.ManageMeetsDialog { Owner = this };
                var pickRes = pick.ShowDialog();
                if (pickRes == true && pick.SelectedMeet != null)
                {
                    // Set full object to power XAML bindings like CurrentMeet.Description/Date
                    vm.CurrentMeet = pick.SelectedMeet;
                }
                else
                {
                    MessageBox.Show("Select a meet first.", "New Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // Open Manage Events dialog for the current meet (like ManageMeets)
            var dlg = new AthStitcher.Views.ManageEventsDialog(vm.CurrentMeet.Id) { Owner = this };
            dlg.vm = this.DataContext as AthStitcherGUI.ViewModels.AthStitcherModel;
            var result = dlg.ShowDialog();
            if (result == true && dlg.SelectedEvent != null)
            {
                vm.CurrentEvent = dlg.SelectedEvent;
                var ctx = new AthStitcherDbContext();
                vm.CurrentHeat = ctx.Heats.FirstOrDefault(h => h.EventId == dlg.SelectedEvent.Id && h.HeatNo == 1);
                
                // Reset lanes/results to the event's lanes if needed (we don't store lanes per event yet; keep current)
                athStitcherViewModel.SetShowSliders(false);
            }
        }
        private void Select_Event_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;

            // Ensure a meet is selected first
            if (vm.CurrentMeet == null)
            {
                var pick = new AthStitcher.Views.ManageMeetsDialog { Owner = this };
                var pickRes = pick.ShowDialog();
                if (pickRes == true && pick.SelectedMeet != null)
                {
                    // Set full object to power XAML bindings like CurrentMeet.Description/Date
                    vm.CurrentMeet = pick.SelectedMeet;
                }
                else
                {
                    MessageBox.Show("Select a meet first.", "New Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // Open Manage Events dialog for the current meet (like ManageMeets)
            var dlg = new AthStitcher.Views.SelectEventDialog(vm.CurrentMeet.Id) { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true && dlg.SelectedEvent != null)
            {
                vm.CurrentEvent = dlg.SelectedEvent;
                using var ctx = new AthStitcherDbContext();
                vm.CurrentHeat = ctx.Heats.FirstOrDefault(h => h.EventId == dlg.SelectedEvent.Id && h.HeatNo == 1);
                
                // Reset lanes/results to the event's lanes if needed (we don't store lanes per event yet; keep current)
                athStitcherViewModel.SetShowSliders(false);
            }
        }

        private void New_Event_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            else
            {

                DateTime meetCutoff = DateTime.Now.Date;
                int cutoff = vm.Scheduling?.EventCutoff ?? 0;
                DateTime meetDate = vm.CurrentMeet?.Date ?? DateTime.Now;
                DateTime eventCutoff = meetDate.AddDays(cutoff);
                if (DateTime.Now.Date > eventCutoff.Date)
                {
                    MessageBox.Show($"Cannot add events after {cutoff} days of the meet date ({meetDate:dd/MM/yyyy}).", "New Event", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Ensure a meet is selected first
            if (vm.CurrentMeet == null)
            {
                var pick = new AthStitcher.Views.ManageMeetsDialog { Owner = this };
                var pickRes = pick.ShowDialog();
                if (pickRes == true && pick.SelectedMeet != null)
                {
                    // Set full object to power XAML bindings like CurrentMeet.Description/Date
                    vm.CurrentMeet = pick.SelectedMeet;
                }
                else
                {
                    MessageBox.Show("Select a meet first.", "New Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            // Ensure a meet is selected first
            if (vm.CurrentMeet == null)
            {
                return;
            }
            int MeetId = vm.CurrentMeet.Id;
            var dlg = new NewEventDialog { Owner = this };
            // Use meet date as base date
            Meet meet;
            using (var ctx = new AthStitcherDbContext())
            {
                meet = ctx.Meets.SingleOrDefault(m => m.Id == MeetId);
                //dlg.BaseDate = meet?.Date;
            }
            if (dlg.ShowDialog() == true)
            {
                using var ctx = new AthStitcherDbContext();
                var ev = dlg._event;
                ev.MeetId = MeetId;

                ctx.Events.Add(ev);
                ctx.SaveChanges();
                vm.CurrentEvent = ev;
                // Ask for number of heats; default and minimum is 1
                int heatsCount = 1;
                var heatsDlg = new NumberOfHeatsDialog { Owner = this };
                if (heatsDlg.ShowDialog() == true)
                {
                    heatsCount = Math.Max(1, heatsDlg.HeatsCount);
                }
                // Create heats 1..heatsCount, skip any that already exist
                for (int h = 1; h <= heatsCount; h++)
                {
                    if (!ctx.Heats.Any(x => x.EventId == ev.Id && x.HeatNo == h))
                    {
                        AddHeat_Menu_Click(sender, e);
                        //ctx.Heats.Add(new Heat { Event = ev, HeatNo = h });
                    }
                }
                //ctx.SaveChanges();
                //vm.CurrentEvent = ev;
                vm.CurrentHeat = ctx.Heats.FirstOrDefault(h => h.EventId == ev.Id && h.HeatNo == 1);
                
            }

        }

        private void Next_Event_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                if (vm.CurrentMeet == null)
                {
                    MessageBox.Show("Select a Meet first.", "Next Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentEvent == null)
                {
                    MessageBox.Show("Select an Event first.", "Next Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                UpdateCurrentEventHeats();
                vm.AdvanceEventNumber();
                //using var ctx = new AthStitcherDbContext();
                
            }
        }

        private void Prev_Event_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                if (vm.CurrentMeet == null)
                {
                    MessageBox.Show("Select a Meet first.", "Previous Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentEvent == null)
                {
                    MessageBox.Show("Select an Event first.", "Previous Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                UpdateCurrentEventHeats();
                vm.DecrementEventNumber();
                //using var ctx = new AthStitcherDbContext();
                
            }
        }

        private void Next_Heat_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                if (vm.CurrentMeet == null)
                {
                    MessageBox.Show("Select a Meet first.", "Next Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentEvent == null)
                {
                    MessageBox.Show("Select an Event first.", "Next Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentHeat == null)
                {
                    MessageBox.Show("Select a Heat  first.", "Next Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }


                UpdateCurrentHeatResults();
                vm.AdvanceHeatNumber();
                
                
            }
        }

        private void UpdateCurrentHeatResults()
        {
            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                if (vm.CurrentMeet == null)
                {
                    MessageBox.Show("Select a Meet first.", "Update Current Heat Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentEvent == null)
                {
                    MessageBox.Show("Select an Event first.", "Update Current Heat Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentHeat == null)
                {
                    MessageBox.Show("Select a Heat  first.", "Update Current Heat Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentHeat.IsHeatDirty())
                {
                    using var ctx = new AthStitcherDbContext();
                    var results = vm.CurrentHeat.Results;
                    ctx.UpdateRange(results);
                    ctx.SaveChanges();
                    var resultsRes = ctx.Results
                        .AsNoTracking()
                        .Where(r => r.HeatId == vm.CurrentHeat.Id)
                        .OrderBy(r => r.Lane) // ascending; use .OrderByDescending if needed
                        .ToList();
                }
            }
        }


        private void UpdateCurrentEventHeats()
        {
            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                if (vm.CurrentMeet == null)
                {
                    MessageBox.Show("Select a Meet first.", "Update Current Heat Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentEvent == null)
                {
                    MessageBox.Show("Select an Event first.", "Update Current Heat Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentHeat != null)
                {
                    UpdateCurrentHeatResults();
                }
                using var ctx = new AthStitcherDbContext();
                var ev = ctx.Events
                .Include(e => e.Heats)
                .First(e => e.Id == vm.CurrentEvent.Id);
                if (ev.IsEventDirty())
                {
                    var heats = ev.Heats;
                    ctx.UpdateRange(heats);
                    ctx.SaveChanges();
                    var resultsRes = ctx.Heats
                        .AsNoTracking()
                        .Where(r => r.EventId == vm.CurrentEvent.Id)
                        .OrderBy(r => r.HeatNo) // ascending; use .OrderByDescending if needed
                        .ToList();
                }
            }
        }
 

        private void Prev_Heat_Button_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AthStitcherGUI.ViewModels.AthStitcherModel vm)
            {
                if (vm.CurrentMeet == null)
                {
                    MessageBox.Show("Select a Meet first.", "Previous Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentEvent == null)
                {
                    MessageBox.Show("Select an Event first.", "Previous Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (vm.CurrentHeat == null)
                {
                    MessageBox.Show("Select a Heat  first.", "Previous Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                UpdateCurrentHeatResults();
                vm.DecrementHeatNumber();
                //using var ctx = new AthStitcherDbContext();
                
            }
        }
        private void AddHeat_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            if (vm.CurrentMeet == null)
            {
                MessageBox.Show("Select a Meet first.", "Add Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (vm.CurrentEvent == null)
            {
                MessageBox.Show("Select an Event first.", "Add Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var ctx = new AthStitcher.Data.AthStitcherDbContext();
                var eventId = vm.CurrentEvent.Id;
                // Determine next heat number
                int nextHeatNo = 1;
                var existingMax = ctx.Heats
                    .Where(h => h.EventId == eventId)
                    .Select(h => (int?)h.HeatNo)
                    .Max();
                if (existingMax.HasValue && existingMax.Value > 0)
                    nextHeatNo = existingMax.Value + 1;

                // Create and save new heat
                var heat = new AthStitcher.Data.Heat { EventId = vm.CurrentEvent.Id, HeatNo = nextHeatNo };
                ctx.Heats.Add(heat);
                ctx.SaveChanges();
                AddResultsToHeat(vm, ctx, vm.CurrentEvent, heat);
                // Make it current in the VM
                vm.CurrentHeat = heat;
                

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add heat: {ex.Message}", "Add Heat", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddResultsToHeat(AthStitcherGUI.ViewModels.AthStitcherModel vm, AthStitcher.Data.AthStitcherDbContext ctx, AthStitcher.Data.Event _event, Heat heat)
        {
            if (vm.CurrentMeet == null)
            {
                MessageBox.Show("Select a Meet first.", "Add Results To Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_event == null)
            {
                MessageBox.Show("Select an Event first.", "Add Results To Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (heat == null)
            {
                MessageBox.Show("Select a Heat first.", "Add Results To Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            //heat.Results = new List<LaneResult>();
            if (_event != null)
            {
                int max = _event.MaxLane ?? 8;
                int min = _event.MinLane ?? 1;
                for (int lane = min; lane <= max; lane++)
                {
                    heat.Results.Add(new LaneResult { HeatId = heat.Id, Lane = lane });
                };
                ctx.SaveChanges();
                //GetCurrentResultsforCurrentHeat(vm, ctx, heat);
            }
        }

        /// <summary>
        /// Remove last heat for current event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveHeat_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            if (vm.CurrentMeet == null)
            {
                MessageBox.Show("Select a Meet first.", "Remove Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (vm.CurrentEvent == null)
            {
                MessageBox.Show("Select an Event first.", "Remove Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (vm.CurrentHeat == null)
            {
                MessageBox.Show("Select a Heat first.", "Remove Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            try
            {
                using var ctx = new AthStitcher.Data.AthStitcherDbContext();
                var eventId = vm.CurrentEvent.Id;

                // Find the last heat for this event ordered by Time (most recent)
                var lastHeat = ctx.Heats
                    .Where(h => h.EventId == eventId)
                    .OrderByDescending(h => h.HeatNo)
                    .FirstOrDefault();

                if (lastHeat == null)
                {
                    MessageBox.Show("No heats found for this event.", "Remove Heat", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Remove the found heat
                ctx.Heats.Remove(lastHeat);
                ctx.SaveChanges();

                // Update current heat number to the highest remaining heat number or 1 if none remain
                var remainingMax = ctx.Heats
                    .Where(h => h.EventId == eventId)
                    .Select(h => (int?)h.HeatNo)
                    .Max();

                var CurrentHeatNumber = remainingMax.HasValue ? remainingMax.Value : -1;
                if (CurrentHeatNumber == -1)
                {
                    // Removed one and only heat so add a new one.
                    AddHeat_Menu_Click(sender, e);
                    return;
                }
                var CurrentHeat = ctx.Heats
                    .FirstOrDefault(h => h.EventId == eventId && h.HeatNo == CurrentHeatNumber);
                //(vm, ctx, CurrentHeat);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to remove last Heat: {ex.Message}", "Remove Heat", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetAppCutoffs_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (/*this.DataContext*/ athStitcherViewModel.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;

            var xx = athStitcherViewModel.DataContext;
            // Ensure Scheduling is initialized
            vm.Scheduling ??= new AthStitcher.Data.Scheduling();

            var dlg = new AthStitcher.Views.SchedulingDialog(vm.Scheduling) { Owner = this };
            var result = dlg.ShowDialog();
            if (result == true)
            {
                // Apply edited model back to VM
                vm.Scheduling.MeetCutoff = dlg.Model.MeetCutoff;
                vm.Scheduling.EventCutoff = dlg.Model.EventCutoff;
                vm.Scheduling.CanAddHeatsOnDayOfMeet = dlg.Model.CanAddHeatsOnDayOfMeet;

                // Persist through existing VM save pipeline
                athStitcherViewModel.SaveViewModel(vm);
            }
        }
        #endregion

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AthStitcherModel vm)
            {

                if (sender is MenuItem menu)
                {
                    if (!string.IsNullOrEmpty(menu.Header.ToString()))
                    {
                        string menuHeader = menu.Header.ToString();
                        string meetHdr = $"{vm.CurrentMeet}\n";

                        switch (menuHeader)
                        {
                            case "Current Heat":
                                if (vm.CurrentHeat.Results != null)
                                {
                                    // Print Current Heat
                                    meetHdr += PrintHeat(vm.CurrentMeet, vm.CurrentEvent, vm.CurrentHeat);
                                }
                                else
                                {
                                    MessageBox.Show("Select an Event Heat first", "Print Heat", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                break;
                            case "Current Event":
                                if (vm.CurrentEvent != null)
                                {
                                    using var ctx = new AthStitcherDbContext();
                                    var ev = ctx.Events
                                    .Include(e => e.Heats)
                                    .First(e => e.Id == vm.CurrentEvent.Id);
                                    string res = "";
                                    //Print Event.Heats
                                    foreach (var heat in ev.Heats)
                                    {
                                        string ht = PrintHeat(vm.CurrentMeet, vm.CurrentEvent, heat);
                                        res += ht + "\n";
                                    }
                                    meetHdr += res;
                                }
                                else
                                {
                                    MessageBox.Show("Select an Event with Heats first", "Print Event Results", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                break;
                            case "Current Meet":
                                if (vm.CurrentMeet != null)
                                {
                                    using var ctx = new AthStitcherDbContext();
                                    var ev = ctx.Meets
                                    .Include(e => e.Events)
                                    .First(e => e.Id == vm.CurrentMeet.Id);
                                    // Print Heats for Meet.Events
                                    foreach (AthStitcher.Data.Event _event in ev.Events)
                                    {
                                        var eEvent = ctx.Events
                                            .Include(eEvent => eEvent.Heats)
                                            .First(eEvent => eEvent.Id == _event.Id);
                                        foreach (Heat heat in eEvent.Heats)
                                        {
                                            meetHdr += PrintHeat(vm.CurrentMeet, _event, heat);
                                        }
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Select a Meet with Events and Heats first", "Print Meet Results", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                break;
                        }

                        if (!string.IsNullOrEmpty(meetHdr))
                        {
                            Clipboard.SetText(meetHdr);
                            MessageBox.Show("Results - Copied to Clipboard", menuHeader, MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                    }

                }
            }
        }

        string PrintHeat(Meet CurrentMeet, AthStitcher.Data.Event CurrentEvent, Heat CurrentHeat)
        {
            if (athStitcherViewModel.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return "";
            string printHeader = "\n";

            printHeader = $"{CurrentEvent}\t" +
                                        $"Heat No: {CurrentHeat.HeatNo}\n" +
                                        "------------------------------------------------------";
            bool tabbed = vm.Scheduling.UseTabbedPrinting;
            List<LaneResult> results = CurrentHeat.Results
                .OrderBy(r => r.ResultSeconds ?? double.MaxValue)  // nulls last
                .ToList();
            if (tabbed)
            {
                printHeader += $"\nPosn\t{LaneResult.TabHeader()}";
                int posn = 1;
                foreach (var lr in results)
                {
                    printHeader += "\n" + $"{posn++}\t{lr.ToTab()}";
                }
            }
            else
            {
                int posn = 1;
                printHeader += $"\nPosn,{LaneResult.CSVHeader()}";
                foreach (var lr in results)
                {
                    printHeader += "\n" + $"{posn++},{lr.ToCSV()}";
                }
            }

            printHeader += "\n\n";
            return printHeader;
        }



        private void SaveHeat_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Print_PDF_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AthStitcherModel vm)
            {

                if (sender is MenuItem menu)
                {
                    if (!string.IsNullOrEmpty(menu.Header.ToString()))
                    {
                        string menuHeader = menu.Header.ToString();
                        string meetHdr = $"{vm.CurrentMeet}\n";

                        switch (menuHeader)
                        {
                            case "Current Heat":
                                if (vm.CurrentHeat.Results != null)
                                {
                                    // Print Current Heat
                                    PrintOneHeatAsPdf(vm.CurrentMeet, vm.CurrentEvent, vm.CurrentHeat);
                                }
                                else
                                {
                                    MessageBox.Show("Select an Event Heat first", "Export Heat Results as Pdf", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                break;
                            case "Current Event":
                                if (vm.CurrentEvent != null)
                                {
                                    PrintOneEventAsPdf(vm.CurrentMeet, vm.CurrentEvent);
                                }
                                else
                                {
                                    MessageBox.Show("Select an Event with Heats first", "Export Event Results as Pdf", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                break;
                            case "Current Meet":
                                if (vm.CurrentMeet != null)
                                {
                                    PrintOneMeetAsPdf(vm.CurrentMeet);
                                }
                                else
                                {
                                    MessageBox.Show("Select an Meet with Events first", "Export Meet Results to Pdf", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                                break;
                        }

                    }

                }
            }
        }

        void PrintOneHeatAsPdf(Meet CurrentMeet, AthStitcher.Data.Event CurrentEvent, Heat CurrentHeat)
        {
            if (athStitcherViewModel.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            // inside your existing Print_Click handler, e.g. in the "Current Heat" case
            if (CurrentHeat != null)
            {
                string filename = $"{vm.CurrentMeet.Description}_Round-{vm.CurrentMeet.Round}_{vm.CurrentEvent}_Heat-{CurrentHeat.HeatNo}.pdf"
                        .Replace(' ', '_').Replace(':', '-');
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = filename  
                    //$"{vm.CurrentMeet.Description}_Round-{vm.CurrentMeet.Round}_{vm.CurrentEvent}_Heat-{CurrentHeat.HeatNo}.pdf"
                };//// 
                if (dlg.ShowDialog() == true)
                {
                    try
                    {
                        // Use the stitched image if available from the viewmodel (optional)
                        //? stitchedImage = null;
                        //try { stitchedImage = athStitcherViewModel.GetOutputPath(); } catch { stitchedImage = null; }
                        PdfExporter.ExportHeatToPdf(vm, CurrentHeat, dlg.FileName); //, stitchedImage);
                        MessageBox.Show($"PDF exported: {dlg.FileName}. File path saved to Clipboard.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a Heat first", "Print Heat results as Pdf", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return;
        }

        void PrintOneEventAsPdf(Meet CurrentMeet, AthStitcher.Data.Event CurrentEvent)
        {
            if (athStitcherViewModel.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            using var ctx = new AthStitcherDbContext();
            var ev = ctx.Events
                .Include(e => e.Heats)
                .ThenInclude(h => h.Results)
                .First(e => e.Id == CurrentEvent.Id);
            string filename = $"{CurrentMeet.Description}_{CurrentMeet.Round}_{ev}_Event.pdf"
                .Replace(' ', '_').Replace(':', '-'); ;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = filename
                //$"{vm.CurrentMeet.Description}_{vm.CurrentMeet.Round}_{ev}_Event.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    PdfExporter.ExportEventToPdf(vm, ev, dlg.FileName);
                    MessageBox.Show($"Event PDF generated: {dlg.FileName}. File path saved to Clipboard.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export Event to PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        void PrintOneMeetAsPdf(Meet CurrentMeet)
        {
            if (athStitcherViewModel.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm)
                return;
            using var ctx = new AthStitcherDbContext();
            var meetWithDetails = ctx.Meets
                .Include(e => e.Events)
                .ThenInclude(h => h.Heats)
                .ThenInclude(r => r.Results)
                .First(e => e.Id == CurrentMeet.Id);
            string filename = $"{CurrentMeet.Description}_{vm.CurrentMeet.Round}_Meet.pdf"
                .Replace(' ', '_').Replace(':', '-'); ;
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = filename
                //$"{vm.CurrentMeet.Description}_{vm.CurrentMeet.Round}_{ev}_Event.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    PdfExporter.ExportMeetToPdf(meetWithDetails, dlg.FileName);
                    MessageBox.Show($"Meet PDF generated: {dlg.FileName}. File path saved to Clipboard.", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export Meet to PDF: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportEventFields2CSV(object sender, RoutedEventArgs e)
        {
            string eventHeader = "Time,Distance,TrackType,MinLane,MaxLane,Gender,AgeGrouping,UnderAgeGroup,MastersAgeGroup,Description";
            try { System.Windows.Clipboard.SetText(eventHeader); } catch { }
            MessageBox.Show($"Created as CSV string: {eventHeader}",
                "Event Fields as CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportEventFields2Tabbed(object sender, RoutedEventArgs e)
        {
            string eventHeader = "Time\tDistance\tTrackType\tMinLane\tMaxLane\tGender\tAgeGrouping\tUnderAgeGroup\tMastersAgeGroup\tDescription";
            try { System.Windows.Clipboard.SetText(eventHeader); } catch { }
            MessageBox.Show($"Created as Tabbed string: {eventHeader}",
                "Event Fields as Tabbed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void ImportEventsfromCSVTextFile(object sender, RoutedEventArgs e)
        {
            ImportEvents(',');
        }

        private void ImportEventsfromTextFileTabbed(object sender, RoutedEventArgs e)
        {
            ImportEvents('\t');
        }

        private void ImportEvents(char del)
        {
            
            if (this.DataContext is not AthStitcherGUI.ViewModels.AthStitcherModel vm )
            {
                MessageBox.Show("Select a Meet first before importing events.", "Import Events", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Ensure a meet is selected first
            if (vm.CurrentMeet == null)
            {
                var pick = new AthStitcher.Views.ManageMeetsDialog { Owner = this };
                var pickRes = pick.ShowDialog();
                if (pickRes == true && pick.SelectedMeet != null)
                {
                    // Set full object to power XAML bindings like CurrentMeet.Description/Date
                    vm.CurrentMeet = pick.SelectedMeet;
                }
                else
                {
                    MessageBox.Show("Select a meet first.", "New Event", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            var dlg = new OpenFileDialog
            {
                Title = "Select Csv/Tabbed file containing events",
                Filter = "CSV Files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true)
                return;
            
            string path = dlg.FileName;
            if (!File.Exists(path))
            {
                MessageBox.Show($"File not found: {path}", "Import Events", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            int imported = 0;
            var errors = new List<string>();
            
            try
            {
                
                var lines = File.ReadAllLines(path)
                                .Select(l => l.Trim())
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();
                if (lines.Count == 0)
                {
                    MessageBox.Show("Csv/Tabbed file is empty.", "Import Events", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Expect header on first line
                string header = lines[0];
                var cols = header.Split(del).Select(h => h.Trim()).ToArray();
                // Minimal header check (we accept variations but require at least Description column)
                if (!cols.Any(c => c.Equals("Distance", StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("CSV header must include a 'Distance' column.", "Import Events", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (!cols.Any(c => c.Equals("Time", StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("CSV header must include a 'Time' column.", "Import Events", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (!cols.Any(c => c.Equals("Gender", StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("CSV header must include a 'Gender' column.", "Import Events", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else if (!cols.Any(c => c.Equals("TrackType", StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("CSV header must include a 'TrackType' column.", "Import Events", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                    using var ctx = new AthStitcherDbContext();
                // Determine next event number for this meet
                int meetId = vm.CurrentMeet.Id;
                var maxEventNumber = ctx.Events
                    .Where(ev => ev.MeetId == meetId)
                    .Max(ev => (int?)ev.EventNumber);

                DateTime meetDate = vm.CurrentMeet.Date??DateTime.MinValue;

                int nextEventNumber = (maxEventNumber ?? 0) + 1;

                // Process data rows
                for (int i = 1; i < lines.Count; i++)
                {
                    var row = lines[i];
                    // allow quoted commas by using simple split on ',' (assumes CSV is simple). If quoted CSVs required use a CSV parser.
                    var parts = row.Split(del).Select(p => p.Trim()).ToArray();

                    try
                    {
                        var ev = new AthStitcher.Data.Event();

                        // Map columns by name where possible
                        for (int c = 0; c < Math.Min(cols.Length, parts.Length); c++)
                        {
                            var colName = cols[c].Trim();
                            var val = parts[c].Trim();

                            if (string.IsNullOrEmpty(val)) continue;

                            switch (colName.ToLowerInvariant())
                            {
                                case "time":
                                    // Try parse DateTime or time-of-day
                                    if (DateTime.TryParse(val, out DateTime dt))
                                    {
                                        ev.Time = dt;
                                    }
                                    else if (TimeSpan.TryParse(val, out TimeSpan ts))
                                    {
                                        ev.Time = DateTime.Today.Add(ts);
                                    }
                                    else if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out double sec))
                                    {
                                        ev.Time = DateTime.Today.AddSeconds(sec);
                                    }
                                    var d = DateOnly.FromDateTime(meetDate);
                                    var t = TimeOnly.FromDateTime(ev.Time??DateTime.MinValue);
                                    ev.Time = d.ToDateTime(t, DateTimeKind.Local); // or Utc/Unspecified
                                    break;
                                case "distance":
                                    if (int.TryParse(val, out int dist)) ev.Distance = dist;
                                    break;
                                case "minlane":
                                    if (int.TryParse(val, out int minLane)) ev.MinLane = minLane;
                                    break;
                                case "maxlane":
                                    if (int.TryParse(val, out int maxLane)) ev.MaxLane = maxLane;
                                    break;
                                case "lanes":
                                    // Accept formats like "1-3", " (1 - 3) ", "1 -3", etc.\
                                    var lanesLower = val.ToLower();
                                    var lanes = lanesLower.Replace("lanes","").Replace("(", "").Replace(")", "").Replace(" ", "");
                                    var m = Regex.Match(lanes, @"^(\d+)-(\d+)$");
                                    if (m.Success)
                                    {
                                        if (int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int min) &&
                                            int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int max))
                                        {
                                            // Validate sensible lane range and ordering (adjust maxLaneLimit if different)
                                            const int maxLaneLimit = 8;
                                            if (min > 0 && max >= min && max <= maxLaneLimit)
                                            {
                                                ev.MinLane = min;
                                                ev.MaxLane = max;
                                            }
                                        }
                                    }
                                    break;
                                case "gender":
                                    if (Enum.TryParse(typeof(AthStitcher.Data.Gender), val, true, out object g))
                                        ev.Gender = (AthStitcher.Data.Gender)g;
                                    break;
                                case "agegrouping":
                                    if (Enum.TryParse(typeof(AthStitcher.Data.AgeGrouping), val, true, out object ag))
                                        ev.AgeGrouping = (AthStitcher.Data.AgeGrouping)ag;
                                    break;
                                case "underagegroup":
                                    if (Enum.TryParse(typeof(AthStitcher.Data.UnderAgeGroup), val, true, out object uag))
                                        ev.UnderAgeGroup = (AthStitcher.Data.UnderAgeGroup)uag;
                                    break;
                                case "mastersagegroup":
                                    if (Enum.TryParse(typeof(AthStitcher.Data.MastersAgeGroup), val, true, out object mag))
                                        ev.MastersAgeGroup = (AthStitcher.Data.MastersAgeGroup)mag;
                                    break;
                                case "description":
                                    ev.Description = val;
                                    break;
                                case "minlane/maxlane":
                                case "maxmin":
                                case "max-min":
                                    // ignore
                                    break;
                                default:
                                    // Unknown column - ignore
                                    break;
                            }
                        }

                        // Fill defaults if needed
                        if (!ev.MinLane.HasValue && vm.CurrentEvent != null)
                            ev.MinLane = vm.CurrentEvent.MinLane;
                        if (!ev.MaxLane.HasValue && vm.CurrentEvent != null)
                            ev.MaxLane = vm.CurrentEvent.MaxLane;

                        ev.MeetId = meetId;
                        ev.EventNumber = nextEventNumber++;
                        // Basic validation: require Description
                        //if (string.IsNullOrWhiteSpace(ev.Description))
                        //{
                        //    errors.Add($"Line {i + 1}: missing Description - skipped.");
                        //    continue;
                        //}

                        ctx.Events.Add(ev);
                        imported++;
                    }
                    catch (Exception exRow)
                    {
                        errors.Add($"Line {i + 1}: {exRow.Message}");
                    }
                }

                ctx.SaveChanges();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import events: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"Imported {imported} events.");
            if (errors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Errors:");
                foreach (var err in errors.Take(10))
                    sb.AppendLine(err);
                if (errors.Count > 10)
                    sb.AppendLine($"... and {errors.Count - 10} more.");
            }

            MessageBox.Show(sb.ToString(), "Import Events Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh VM current meet/events if needed
            try
            {
                athStitcherViewModel.LoadViewModel(); ; // reload model so UI shows imported events (LoadViewModel exists earlier)
                this.DataContext = athStitcherViewModel.DataContext;
            }
            catch { }
            
        }
        ////////////////////////////////////////////////////////////////////////////////////////
    }
}
