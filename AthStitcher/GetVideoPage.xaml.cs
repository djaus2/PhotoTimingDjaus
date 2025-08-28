using GetVideoWPFLib.Services;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

// Add alias to avoid ambiguity between System.Windows.Forms.Application and System.Windows.Application
using WpfApplication = System.Windows.Application;

namespace AthStitcherGUI
{
    public partial class GetVideoPage : Window
    {
        internal GetVideoWPFLib.ViewModels.VideoDownloadViewModel VideoDownloadViewModel { get; set; }
        public GetVideoPage()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            this.DataContext = VideoDownloadViewModel;  //My change here.
        }



        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            // Use the alias to avoid ambiguity
           // WpfApplication.Current.Shutdown();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowAboutDialog();
        }

        private void ShowAboutDialog()
        {
            var aboutWindow = new Window
            {
                Title = "About Video Download Sample",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(20)
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Title
            var titleBlock = new TextBlock
            {
                Text = "Video Download Sample",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
            };
            stackPanel.Children.Add(titleBlock);

            // Description
            var descriptionBlock = new TextBlock
            {
                Text = "A WPF application demonstrating the use of GetVideoWPFLib for downloading videos over TCP.",
                FontSize = 14,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(descriptionBlock);

            // Features
            var featuresTitle = new TextBlock
            {
                Text = "Features:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            stackPanel.Children.Add(featuresTitle);

            var featuresText = new TextBlock
            {
                Text = "• In-app TCP Listening using GetVideoWPFLib\n" +
                       "• Video Download Reception\n" +
                       "• Download Folder Management\n" +
                       "• Real-time File Monitoring\n" +
                       "• Download Progress Indicators",
                FontSize = 12,
                Margin = new Thickness(20, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };
            stackPanel.Children.Add(featuresText);

            // Version Info
            var versionBlock = new TextBlock
            {
                Text = "Version: 1.0\nFramework: .NET 9.0",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 20),
                TextAlignment = TextAlignment.Center
            };
            stackPanel.Children.Add(versionBlock);

            // Links Section
            var linksTitle = new TextBlock
            {
                Text = "Links:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(linksTitle);

            // Blog Site Link
            var blogLinkBlock = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 5)
            };
            
            var blogRun1 = new Run("Blog Site: ");
            var blogHyperlink = new Hyperlink(new Run("https://davidjones.sportronics.com.au"))
            {
                NavigateUri = new Uri("https://davidjones.sportronics.com.au"),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
            };
            blogHyperlink.RequestNavigate += (s, e) => 
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
            
            blogLinkBlock.Inlines.Add(blogRun1);
            blogLinkBlock.Inlines.Add(blogHyperlink);
            stackPanel.Children.Add(blogLinkBlock);

            // GitHub Repository Link
            var githubLinkBlock = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 20)
            };
            
            var githubRun1 = new Run("GitHub Repository: ");
            var githubHyperlink = new Hyperlink(new Run("https://github.com/djaus2/TransferVideoOverTcp"))
            {
                NavigateUri = new Uri("https://github.com/djaus2/TransferVideoOverTcp"),
                Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243))
            };
            githubHyperlink.RequestNavigate += (s, e) => 
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
            
            githubLinkBlock.Inlines.Add(githubRun1);
            githubLinkBlock.Inlines.Add(githubHyperlink);
            stackPanel.Children.Add(githubLinkBlock);

            // OK Button
            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            okButton.Click += (s, e) => aboutWindow.Close();
            stackPanel.Children.Add(okButton);

            scrollViewer.Content = stackPanel;
            aboutWindow.Content = scrollViewer;
            aboutWindow.ShowDialog();
        }
    }
}
