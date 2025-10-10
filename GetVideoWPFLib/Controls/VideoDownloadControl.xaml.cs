using GetVideoWPFLib.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace GetVideoWPFLib.Controls
{
    public partial class VideoDownloadControl : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                "ViewModel", 
                typeof(VideoDownloadViewModel), 
                typeof(VideoDownloadControl), 
                new PropertyMetadata(null, OnViewModelChanged));

        public VideoDownloadViewModel ViewModel
        {
            get { return (VideoDownloadViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VideoDownloadControl control && e.NewValue is VideoDownloadViewModel viewModel)
            {
                control.DataContext = viewModel;
                control.InitializeControl();
            }
        }

        public VideoDownloadControl()
        {
            InitializeComponent();
        }

        private void InitializeControl()
        {
            // Wire up the Browse button click event if not already done
            SelectButton.Click -= SelectButton_Click; // Remove any existing handlers
            SelectButton.Click += SelectButton_Click;
            
            // Ensure DataContext is set to ViewModel
            if (ViewModel != null && DataContext != ViewModel)
            {
                DataContext = ViewModel;
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFolder();
        }

        private void SelectFolder()
        {
            // Get the ViewModel from DataContext if it's null
            if (ViewModel == null && DataContext is VideoDownloadViewModel viewModel)
            {
                ViewModel = viewModel;
            }
            
            if (ViewModel == null)
            {
                System.Windows.MessageBox.Show("Cannot select folder: ViewModel is not available.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var dialog = new FolderBrowserDialog
            {
                Description = "Select Download Folder",
                UseDescriptionForTitle = true,
                SelectedPath = ViewModel.DownloadFolder
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.DownloadFolder = dialog.SelectedPath;
                ViewModel.RefreshDownloadedFilesCommand.Execute(null);
            }
        }


    }
}
