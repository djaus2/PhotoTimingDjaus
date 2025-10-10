using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;
using System.Security.Cryptography;
using Sportronics.VideoEnums;
using Xceed.Wpf.Toolkit;
using System.IO;

namespace DownloadVideoOverTCPLib
{
    public static class GetVideo
    {
        const int ChunkSize = 1024 * 1024; // 1MB
    public static string Download(string fileFolder = @"C:\temp",int port = 5000 )
    {
        // Display local IP address
        string? localIP = Dns.GetHostEntry(Dns.GetHostName())
                            .AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

        if (string.IsNullOrEmpty(localIP))
        {
                System.Diagnostics.Debug.WriteLine("Local IP address not found. Please check your network connection.");
            return ""; // Exit early — no point starting the server
        }
        else
        {
                System.Diagnostics.Debug.WriteLine($"Listening on IP: {localIP}, Port: {port}");
        }


        try
        {
            if (!Directory.Exists(fileFolder))
                Directory.CreateDirectory(fileFolder);

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
                System.Diagnostics.Debug.WriteLine("Waiting for connection...");

            using var client = listener.AcceptTcpClient();
                System.Diagnostics.Debug.WriteLine("Client connected - starting download...");
            using var networkStream = client.GetStream();

            //Get meta info including filename and checksum
            byte[] jsonLengthBytes = new byte[sizeof(int)];
            ReadExact(networkStream, jsonLengthBytes, 0, sizeof(int));
            int jsonLength = BitConverter.ToInt32(jsonLengthBytes, 0);

            System.Diagnostics.Debug.WriteLine($"JSON length: {jsonLength} bytes");

            byte[] jsonBuffer = new byte[jsonLength];
            ReadExact(networkStream, jsonBuffer, 0, jsonLength);
            string json = Encoding.UTF8.GetString(jsonBuffer);
           System.Diagnostics.Debug.WriteLine($"Received JSON: {json}");
                VideoInfo videoInfo = VideoInfo.CreateFromJson(json);

            // Get info file
            string fileName = videoInfo.Filename; // Encoding.UTF8.GetString(nameBuffer);
            string infoFilePath = Path.Combine(fileFolder, Path.GetFileNameWithoutExtension(fileName) + ".json");
            File.WriteAllText(infoFilePath, json);
            System.Diagnostics.Debug.WriteLine($"Receiving file: {fileName}");

            // Get Video and compare Checksums
            byte[]    expectedChecksum = videoInfo.Checksum; // new byte[32]; // SHA256

            string filePath = Path.Combine(fileFolder, fileName);
            using var fileStream = File.Create(filePath);
            using var sha256 = SHA256.Create();

            System.Diagnostics.Debug.WriteLine("Downloading video data...");
            byte[] buffer = new byte[1024 * 1024];
            int bytesRead;
            long totalBytes = 0;
            while ((bytesRead = networkStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                totalBytes += bytesRead;
            }
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            System.Diagnostics.Debug.WriteLine($"Download completed. Total bytes: {totalBytes:N0}");

            byte[] actualChecksum = sha256.Hash!;



            bool isValid = expectedChecksum.SequenceEqual(actualChecksum);
            System.Diagnostics.Debug.WriteLine($"Expected Checksum: {BitConverter.ToString(expectedChecksum).Replace("-", "")}");
            System.Diagnostics.Debug.WriteLine($"Actual Checksum: {BitConverter.ToString(actualChecksum).Replace("-", "")}");
            System.Diagnostics.Debug.WriteLine(isValid ? "✅ File received successfully" : "❌ Checksum mismatch!");
            if(!isValid)
            {
                    File.Delete(filePath);
            }

            // All god if isValid

            listener.Stop();
            return fileName;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during transfer: {ex.Message}");
            return "";
        }
    }   
    public static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
                if (bytesRead == 0)
                    throw new EndOfStreamException("Stream ended before reading expected bytes.");
                totalRead += bytesRead;
            }
        }
    }
}
