
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using GetVideoWPFLib.Services;
    using GetVideoWPFLib.ViewModels;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;

    // Add alias to avoid ambiguity between System.Windows.Forms.Application and System.Windows.Application
    using WpfApplication = System.Windows.Application;
    // Add alias to avoid ambiguity between System.Windows.Forms.MessageBox and System.Windows.MessageBox
    using WpfMessageBox = System.Windows.MessageBox;

    namespace AthStitcherGUI.ViewModels
    {
        public partial class AthStitcherGetVideoViewModel : ObservableObject
        {
            private readonly IConfiguration _configuration;
            private readonly IVideoDownloadService _videoDownloadService;
            private FileSystemWatcher? _fileWatcher;

            [ObservableProperty]
            private VideoDownloadViewModel _videoDownloadViewModel;

            [ObservableProperty]
            private ObservableCollection<string> _downloadedFiles = new();

            [ObservableProperty]
            private string _statusMessage = "Ready";

            [ObservableProperty]
            private bool _isVideoDownloading = false;

            [ObservableProperty]
            private string _currentDownloadFile = "";

            public AthStitcherGetVideoViewModel(IConfiguration configuration, IVideoDownloadService videoDownloadService)
            {
                _configuration = configuration;
                _videoDownloadService = videoDownloadService;

                // Create the VideoDownloadViewModel from the library
                VideoDownloadViewModel = new VideoDownloadViewModel(_configuration, _videoDownloadService);

                // Subscribe to video download service events
                _videoDownloadService.VideoDownloadStarted += OnVideoDownloadListening;
                _videoDownloadService.VideoDownloadCompleted += OnVideoDownloadCompleted;
                _videoDownloadService.VideoDownloadFailed += OnVideoDownloadFailed;

                RefreshDownloadedFiles();
                SetupFileWatcher(VideoDownloadViewModel.DownloadFolder);

                // Subscribe to property changes in the VideoDownloadViewModel
                VideoDownloadViewModel.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(GetVideoWPFLib.ViewModels.VideoDownloadViewModel.DownloadFolder))
                    {
                        SetupFileWatcher(VideoDownloadViewModel.DownloadFolder);
                        RefreshDownloadedFiles();
                    }
                };
            }

            private void OnVideoDownloadListening(object? sender, string filename)
            {
                // Status messages are now handled by the library's VideoDownloadViewModel
                // This event handler is kept for any future app-specific logic
            }

            private void OnVideoDownloadCompleted(object? sender, string filename)
            {
                WpfApplication.Current.Dispatcher.Invoke(() =>
                {
                    // Refresh the file list
                    RefreshDownloadedFiles();
                });
            }

            private void OnVideoDownloadFailed(object? sender, Exception ex)
            {
                // Status messages are now handled by the library's VideoDownloadViewModel
                // This event handler is kept for any future app-specific logic
            }

            [RelayCommand]
            private void RefreshDownloadedFiles()
            {
                try
                {
                    if (Directory.Exists(VideoDownloadViewModel.DownloadFolder))
                    {
                        var files = _videoDownloadService.GetDownloadedFiles(VideoDownloadViewModel.DownloadFolder);

                        DownloadedFiles.Clear();
                        foreach (var file in files)
                        {
                            DownloadedFiles.Add(file);
                        }
                        StatusMessage = $"Found {files.Length} files in {VideoDownloadViewModel.DownloadFolder}";
                    }
                    else
                    {
                        DownloadedFiles.Clear();
                        StatusMessage = $"Download folder does not exist: {VideoDownloadViewModel.DownloadFolder}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error refreshing files: {ex.Message}";
                }
            }

            [RelayCommand]
            private void OpenDownloadFolder()
            {
                try
                {
                    if (Directory.Exists(VideoDownloadViewModel.DownloadFolder))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = VideoDownloadViewModel.DownloadFolder,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        WpfMessageBox.Show(
                            $"Folder does not exist: {VideoDownloadViewModel.DownloadFolder}",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error opening folder: {ex.Message}";
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
                            WpfApplication.Current.Dispatcher.Invoke(() =>
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
        }
    }


