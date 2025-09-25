using System;
using System.Threading;
using System.Threading.Tasks;

namespace GetVideoWPFLib.Services
{
    public interface IVideoDownloadService
    {
        event EventHandler<string> VideoDownloadStarted;
        event EventHandler<string> VideoDownloadCompleted;
        event EventHandler<Exception> VideoDownloadFailed;

        Task StartListeningAsync(string downloadFolder, int port, CancellationToken cancellationToken);
        void StopListening();
        string[] GetDownloadedFiles(string downloadFolder);
    }
}
