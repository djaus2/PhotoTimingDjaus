using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GetVideoWPFLib.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GetVideoWPFLib.ViewModels
{
    public partial class VideoDownloadViewModel : ObservableObject
    {
        private readonly IConfiguration _configuration;
        private readonly IVideoDownloadService _videoDownloadService;
        private FileSystemWatcher? _fileWatcher;
        private CancellationTokenSource? _cancellationTokenSource;

        public event Action? BackRequested;
        public event Action? CloseRequested;

        [ObservableProperty]
        private string _downloadFolder = @"C:\temp\vid";

        [ObservableProperty]
        private int _port = 5000;

        [ObservableProperty]
        private bool _isListening;

        [ObservableProperty]
        private string _localIpAddress = "";

        [ObservableProperty]
        private ObservableCollection<string> _downloadedFiles = new();

        [ObservableProperty]
        private string _statusMessage = "Ready";

        [ObservableProperty]
        private bool _isVideoDownloading = false;

        [ObservableProperty]
        private string _currentDownloadFile = "";

        public VideoDownloadViewModel(IConfiguration configuration, IVideoDownloadService videoDownloadService)
        {
            _configuration = configuration;
            _videoDownloadService = videoDownloadService;
            
            // Load settings from configuration
            if (_configuration != null)
            {
                _downloadFolder = _configuration["VideoSettings:DefaultFolder"] ?? @"C:\temp\vid";
                if (int.TryParse(_configuration["VideoSettings:DefaultPort"], out int configPort))
                {
                    _port = configPort;
                }
            }
            
            // Subscribe to video download service events
            _videoDownloadService.VideoDownloadStarted += OnVideoDownloadStarted;
            _videoDownloadService.VideoDownloadCompleted += OnVideoDownloadCompleted;
            _videoDownloadService.VideoDownloadFailed += OnVideoDownloadFailed;
            
            // Ensure the popup is hidden initially
            IsVideoDownloading = false;
            CurrentDownloadFile = "";
            
            LoadLocalIpAddress();
            RefreshDownloadedFiles();
            SetupFileWatcher(_downloadFolder);
        }

        private void OnVideoDownloadStarted(object? sender, string filename)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                // Never show the popup for VideoDownloadStarted events
                // Only update the status message
                IsVideoDownloading = false;
                CurrentDownloadFile = "";
                StatusMessage = filename;
            });
        }

        private void OnVideoDownloadCompleted(object? sender, string filename)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                // Show the popup with the completed filename
                IsVideoDownloading = true;
                CurrentDownloadFile = filename;
                StatusMessage = $"Download completed: {filename}";
                
                // Keep the popup visible for 3 seconds
                await Task.Delay(3000);
                
                // Hide the popup
                IsVideoDownloading = false;
                CurrentDownloadFile = "";
                
                // Refresh the file list
                RefreshDownloadedFiles();
            });
        }

        private void OnVideoDownloadFailed(object? sender, Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsVideoDownloading = false;
                CurrentDownloadFile = "";
                StatusMessage = $"Download failed: {ex.Message}";
            });
        }

        private void LoadLocalIpAddress()
        {
            try
            {
                LocalIpAddress = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    ?.ToString() ?? "Not available";
            }
            catch (Exception)
            {
                LocalIpAddress = "Not available";
            }
        }

        [RelayCommand]
        private void RefreshDownloadedFiles()
        {
            try
            {
                if (Directory.Exists(DownloadFolder))
                {
                    var files = _videoDownloadService.GetDownloadedFiles(DownloadFolder);
                    
                    DownloadedFiles.Clear();
                    foreach (var file in files)
                    {
                        DownloadedFiles.Add(file);
                    }
                    StatusMessage = $"Found {files.Length} files in {DownloadFolder}";
                }
                else
                {
                    DownloadedFiles.Clear();
                    StatusMessage = $"Download folder does not exist: {DownloadFolder}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing files: {ex.Message}";
            }
        }

        // This method is left for the consumer to implement
        public virtual void BrowseDownloadFolder(string initialPath)
        {
            // To be implemented by the consumer
            // This allows different UI frameworks to provide their own folder browser dialog
        }

        [RelayCommand]
        private void OpenDownloadFolder()
        {
            try
            {
                if (Directory.Exists(DownloadFolder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = DownloadFolder,
                        UseShellExecute = true
                    });
                }
                else
                {
                    // This message should be handled by the consumer
                    StatusMessage = $"Folder does not exist: {DownloadFolder}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening folder: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task StartListeningAsync()
        {
            try
            {
                IsListening = true;
                StatusMessage = "Starting listener...";
                
                await _videoDownloadService.StartListeningAsync(DownloadFolder, Port, CancellationToken.None);
            }
            catch (Exception ex)
            {
                IsListening = false;
                StatusMessage = $"Error starting listener: {ex.Message}";
            }
        }

        [RelayCommand]
        private void StopListening()
        {
            try
            {
                _videoDownloadService.StopListening();
                IsListening = false;
                StatusMessage = "Listener stopped";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping listener: {ex.Message}";
                // Still set IsListening to false since we want the UI to reflect that we're not listening
                IsListening = false;
            }
        }

        private void SetupFileWatcher(string folder)
        {
            // Dispose of existing file watcher if any
            _fileWatcher?.Dispose();
            
            try
            {
                if (Directory.Exists(folder))
                {
                    _fileWatcher = new FileSystemWatcher(folder)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                        EnableRaisingEvents = true,
                        Filter = "*.mp4"
                    };
                    
                    _fileWatcher.Created += (s, e) => 
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        {
                            RefreshDownloadedFiles();
                        });
                    };
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error setting up file watcher: {ex.Message}";
            }
        }

        [RelayCommand]
        private void BackFromDownload()
        {
            // Do NOT stop listening; just signal the host to navigate back
            BackRequested?.Invoke();
        }

        [RelayCommand]
        private void CloseDownload()
        {
            // Stop listening and signal the host to close
            _videoDownloadService.StopListening();
            IsListening = false;
            StatusMessage = "Listener stopped";
            CloseRequested?.Invoke();
        }
    }
}
