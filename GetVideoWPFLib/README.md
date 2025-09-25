# GetVideoWPFLib

A WPF library for downloading videos over TCP, based on the GetVideoInAppWPF application.

## Features

- TCP listener for receiving video files
- User interface components for controlling the download process
- File system monitoring for downloaded files
- Status updates and notifications

## Requirements

- .NET 9.0 or later
- Windows OS (uses WPF)

## Installation

1. Add a reference to the GetVideoWPFLib project in your solution
2. Ensure you have the required NuGet packages:
   - CommunityToolkit.Mvvm (8.2.2 or later)
   - Microsoft.Extensions.DependencyInjection (9.0.0 or later)
   - Microsoft.Extensions.Configuration.Json (9.0.0 or later)

## Usage Example

### 1. Create a New WPF Application

Start by creating a new WPF application project:

1. In Visual Studio, go to File > New > Project
2. Select "WPF Application" template
3. Name your project and click Create
4. Set the target framework to .NET 9.0
5. Add the required NuGet packages:
   ```
   Install-Package CommunityToolkit.Mvvm
   Install-Package Microsoft.Extensions.DependencyInjection
   Install-Package Microsoft.Extensions.Configuration.Json
   ```
6. Add a reference to the GetVideoWPFLib project in your solution

### 2. Register Services

```csharp
// In your App.xaml.cs or startup code
using GetVideoWPFLib.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load configuration
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        Configuration = builder.Build();

        // Configure services
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(Configuration);
        services.AddSingleton<IVideoDownloadService, VideoDownloadService>();
        services.AddSingleton<GetVideoWPFLib.ViewModels.VideoDownloadViewModel>();

        ServiceProvider = services.BuildServiceProvider();
    }
}
```

### 3. Create appsettings.json

```json
{
  "VideoSettings": {
    "DefaultFolder": "C:\\temp\\vid",
    "DefaultPort": 5000
  }
}
```

### 4. Use the VideoDownloadControl in your Window or Page

```csharp
// In your MainWindow.xaml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:getvideolib="clr-namespace:GetVideoWPFLib.Controls;assembly=GetVideoWPFLib"
        Title="Video Downloader" Height="600" Width="800">
    <Grid>
        <getvideolib:VideoDownloadControl x:Name="VideoControl" />
    </Grid>
</Window>
```

```csharp
// In your MainWindow.xaml.cs
using GetVideoWPFLib.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Get the view model from the service provider
        var viewModel = ((App)Application.Current).ServiceProvider.GetRequiredService<VideoDownloadViewModel>();
        
        // Initialize the control with the view model
        VideoControl.Initialize(viewModel);
    }
}
```

## Advanced Usage

### Handling Events

You can subscribe to events from the `IVideoDownloadService` to handle download notifications in your own code:

```csharp
private void SetupVideoDownloadEvents(IVideoDownloadService service)
{
    service.VideoDownloadStarted += (sender, filename) => 
    {
        // Handle download started
    };
    
    service.VideoDownloadCompleted += (sender, filename) => 
    {
        // Handle download completed
    };
    
    service.VideoDownloadFailed += (sender, exception) => 
    {
        // Handle download failure
    };
}
```

### Customizing the Download Folder Browser

The library uses Windows Forms' FolderBrowserDialog by default, but you can implement your own folder selection logic:

```csharp
// Create a custom control that inherits from VideoDownloadControl
public class CustomVideoDownloadControl : GetVideoWPFLib.Controls.VideoDownloadControl
{
    public CustomVideoDownloadControl()
    {
        // Override the browse button click with your own implementation
        BrowseButton.Click -= (s, e) => BrowseFolder();
        BrowseButton.Click += (s, e) => CustomBrowseFolder();
    }
    
    private void CustomBrowseFolder()
    {
        // Implement your own folder browser logic here
        // For example, using Microsoft.WindowsAPICodePack.Dialogs
        
        // Update the view model when done
        ViewModel.DownloadFolder = selectedPath;
        ViewModel.RefreshDownloadedFilesCommand.Execute(null);
    }
}
```

## License

This project is licensed under the Creative Commons CC0-1.0 Universal License - see the [LICENSE](LICENSE) file for details.

CC0-1.0 is a public domain dedication that allows you to freely use, modify, and distribute this code without any restrictions.


