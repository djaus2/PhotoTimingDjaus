using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GetVideoWPFLib.Services
{
    public class VideoDownloadService : IVideoDownloadService
    {
        private readonly IConfiguration _configuration;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listeningTask;

        public event EventHandler<string>? VideoDownloadStarted;
        public event EventHandler<string>? VideoDownloadCompleted;
        public event EventHandler<Exception>? VideoDownloadFailed;

        public bool IsListening => _listeningTask != null && !_listeningTask.IsCompleted;

        public VideoDownloadService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task StartListeningAsync(string folder, int port, CancellationToken cancellationToken)
        {
            if (IsListening)
            {
                return;
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            _listeningTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            if (!Directory.Exists(folder))
                            {
                                Directory.CreateDirectory(folder);
                            }

                            VideoDownloadStarted?.Invoke(this, "Waiting for connection...");

                            string fileName = null;
                            
                            try
                            {
                                var connectionTask = Task.Run(async () => 
                                {
                                    await Task.Delay(500);
                                    // VideoDownloadStarted?.Invoke(this, "File download in progress");
                                });
                                
                                fileName = DownloadVideoOverTCPLib.GetVideo.Download(folder, port);
                                


                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    VideoDownloadCompleted?.Invoke(this, fileName);
                                    if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        await Task.Delay(500, _cancellationTokenSource.Token);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    // No file was downloaded, wait a bit before trying again
                                    if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                                    {
                                        await Task.Delay(1000, _cancellationTokenSource.Token);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                VideoDownloadFailed?.Invoke(this, ex);
                                
                                // Wait a bit before trying again
                                if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                                {
                                    await Task.Delay(2000, _cancellationTokenSource.Token);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            VideoDownloadFailed?.Invoke(this, ex);
                            
                            // Wait a bit before trying again
                            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                await Task.Delay(5000, _cancellationTokenSource.Token);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, do nothing
                }
                catch (Exception ex)
                {
                    VideoDownloadFailed?.Invoke(this, ex);
                }
            });

            await Task.CompletedTask;
        }

        public void StopListening()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _listeningTask = null;
        }

        public string[] GetDownloadedFiles(string downloadFolder)
        {
            if (!Directory.Exists(downloadFolder))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(downloadFolder, "*.mp4")
                .Select(f => Path.GetFileName(f)!)
                .ToArray();
        }

        public void ClearDownloadedFiles(string downloadFolder)
        {
            if (!Directory.Exists(downloadFolder))
            {
                return;
            }
            string msg = "Please confirm that you wish to delete the contents of the folder: {downloadFolder}";
            MessageBoxResult result = System.Windows.MessageBox.Show(
                msg,
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {

                {
                    foreach (var file in Directory.GetFiles(downloadFolder))
                    {

                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore any errors during deletion
                        }
                    }
                }
            }
        }
        
        public void StitchSelectedVideo(string selectedVideo)
        {
            if (!File.Exists(selectedVideo))
            {
                return;
            }
            // Call the external stitching process

        }
    }
}
