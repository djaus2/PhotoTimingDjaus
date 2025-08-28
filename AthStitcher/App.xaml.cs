using AthStitcherGUI;
using System.Configuration;
using System.Data;
using System.Windows;

// Add alias to avoid ambiguity between System.Windows.Forms.Application and System.Windows.Application
using WpfApplication = System.Windows.Application;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using GetVideoWPFLib.Services;
using AthStitcherGUI.ViewModels;
using GetVideoWPFLib.ViewModels;

namespace AthStitcherGUI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    private ServiceProvider _serviceProvider;
    public IConfiguration Configuration { get; private set; }

    public App()
    {
        // Initialize Configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        // Register configuration
        services.AddSingleton<IConfiguration>(Configuration);

        // Register GetVideoWPFLib services
        services.AddSingleton<IVideoDownloadService, VideoDownloadService>();

        // Register view models
        services.AddSingleton<VideoDownloadService>();
    }

    public IVideoDownloadService GetVideoviewModel { get; set; }

    // Add a method to open the GetVideoPage with the proper ViewModel
    public void OpenGetVideoPage()
    {
        var getVideoPage = new GetVideoPage();
        // Create a proper VideoDownloadViewModel instance
        var videoDownloadViewModel = new VideoDownloadViewModel(
            Configuration, 
            (IVideoDownloadService)GetVideoviewModel);
        getVideoPage.VideoDownloadViewModel = videoDownloadViewModel;
        getVideoPage.Show();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        GetVideoviewModel = _serviceProvider.GetRequiredService<VideoDownloadService>();
        //GetVideoPage.VideoDownloadViewModel = GetVideoviewModel;

        // Create main window with view model from DI
        var mainWindow = new MainWindow();

        mainWindow.Show();
    }
}
