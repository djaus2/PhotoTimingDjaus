using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AthStitcher.Network
{
    public static class SendTextFileClient
    {
        /// <summary>
        /// Sends metadata (JSON) then a file over TCP using the receiver protocol:
        /// [4-byte little-endian JSON length][JSON bytes][file bytes...]
        /// Connection is closed by sender when done.
        /// Adds SHA-256 checksum to the metadata (field: Checksum, lowercase hex) so the receiver can verify integrity.
        /// </summary>
        public static async Task SendFileAsync(string hostOrIp, int port, string filePath, string? filenameOverride = null, int connectTimeoutMs = 10000, CancellationToken ct = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(hostOrIp, port);
            if (await Task.WhenAny(connectTask, Task.Delay(connectTimeoutMs, ct)) != connectTask)
                throw new TimeoutException($"Connecting to {hostOrIp}:{port} timed out.");

            using var stream = client.GetStream();

            // Open the file once and compute SHA-256 checksum, then rewind for sending.
            using var fileStream = File.OpenRead(filePath);
            byte[] checksum;
            try
            {
                checksum = await ComputeSha256Async(fileStream, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // Rewind stream to send file contents
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            string checksumHex = BitConverter.ToString(checksum).Replace("-", "").ToLowerInvariant();

            // Build metadata JSON (includes filename, checksum and algorithm, and length)
            var metadata = new
            {
                Filename = filenameOverride ?? Path.GetFileName(filePath),
                Checksum = checksumHex,
                ChecksumAlgorithm = "SHA256",
                FileLength = fileStream.Length
            };
            string json = JsonSerializer.Serialize(metadata);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] jsonLen = BitConverter.GetBytes(jsonBytes.Length); // little-endian on Windows

            // Send JSON length prefix
            await stream.WriteAsync(jsonLen.AsMemory(0, jsonLen.Length), ct).ConfigureAwait(false);
            // Send JSON bytes
            await stream.WriteAsync(jsonBytes.AsMemory(0, jsonBytes.Length), ct).ConfigureAwait(false);

            // Send file contents
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            }

            // Optionally flush and close by disposing client/stream
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }


        public static async Task SendTextAsync(string hostOrIp, int port, string msg, string dataType, int connectTimeoutMs = 10000, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(msg))
                throw new Exception("No text to send");

            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(hostOrIp, port);
            if (await Task.WhenAny(connectTask, Task.Delay(connectTimeoutMs, ct)) != connectTask)
                throw new TimeoutException($"Connecting to {hostOrIp}:{port} timed out.");

            using var stream = client.GetStream();

            // Open the file once and compute SHA-256 checksum, then rewind for sending.
            using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(msg));
            byte[] checksum;
            try
            {
                checksum = await ComputeSha256Async(fileStream, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }

            // Rewind stream to send file contents
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            string checksumHex = BitConverter.ToString(checksum).Replace("-", "").ToLowerInvariant();

            // Build metadata JSON (includes filename, checksum and algorithm, and length)
            var metadata = new
            {
                Filename = dataType + ".txt",
                Checksum = checksumHex,
                ChecksumAlgorithm = "SHA256",
                FileLength = fileStream.Length
            };
            string json = JsonSerializer.Serialize(metadata);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            byte[] jsonLen = BitConverter.GetBytes(jsonBytes.Length); // little-endian on Windows

            // Send JSON length prefix
            await stream.WriteAsync(jsonLen.AsMemory(0, jsonLen.Length), ct).ConfigureAwait(false);
            // Send JSON bytes
            await stream.WriteAsync(jsonBytes.AsMemory(0, jsonBytes.Length), ct).ConfigureAwait(false);

            // Send file contents
            const int bufferSize = 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
            }

            // Optionally flush and close by disposing client/stream
            await stream.FlushAsync(ct).ConfigureAwait(false);
        }

        private static async Task<byte[]> ComputeSha256Async(Stream stream, CancellationToken ct)
        {
            // Ensure start at beginning
            if (stream.CanSeek)
                stream.Position = 0;

            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            byte[] buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
            {
                hasher.AppendData(buffer.AsSpan(0, read));
            }

            return hasher.GetHashAndReset();
        }
    }
}

