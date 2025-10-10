# DownloadVideoOverTcpLib

A .NET library for receiving video files over TCP with checksum verification. This library provides a simple and reliable way to download video files from a sender over a TCP connection.

## Features

- TCP-based file transfer with automatic connection handling
- Checksum verification using SHA-256
  - A Json file with meta info is transferred first
    - Containing the expected checksum and filename
    - Also the Video TimeFrom mode, Video Start Time, and GunTime if relevant.
- Progress reporting during download
- Cross-platform compatibility (.NET 9.0)
- Simple API for easy integration

## Requirements

- .NET 9.0 or later
- Required Custom NuGet Packages:
  - [Sportronics.VideoEnums.Windows V2.0.1](https://www.nuget.org/packages/Sportronics.VideoEnums.Windows/2.0.1)

## Installation

Add a reference to the DownloadVideoOverTcpLib project in your solution or install the NuGet package:

```bash
dotnet add package DownloadVideoOverTcpLib
```

## Usage

### Basic Usage

The simplest way to use the library is to call the `Download` method:

```csharp
using DownloadVideoOverTCPLib;

// Specify download folder and port
string downloadFolder = @"C:\temp\videos";
int port = 5000;

// Start listening and download the file
string fileName = GetVideo.Download(downloadFolder, port);

if (!string.IsNullOrEmpty(fileName))
{
    Console.WriteLine($"File downloaded successfully: {fileName}");
}
else
{
    Console.WriteLine("Download failed");
}
```

### Console Application Example

Here's how to use the library in a console application with configuration support:

```csharp
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using DownloadVideoOverTCPLib;
using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {
        // Load configuration from appsettings.json
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();

        // Get settings from configuration
        string folder = config["AppSettings:Folder"] ?? @"C:\temp\videos";
        int port = int.Parse(config["AppSettings:Port"] ?? "5000");

        // Create folder if it doesn't exist
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        // Display local IP for connecting clients
        string localIP = GetLocalIPAddress();
        Console.WriteLine($"App is running at IP: {localIP}, Port: {port}");
        Console.WriteLine($"Using folder: {folder}");

        // Start download
        var filepath = GetVideo.Download(folder, port);
        
        Console.WriteLine($"File received: {filepath} in folder {folder}");
    }

    static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "No network adapters with an IPv4 address in the system!";
    }
}
```

### Configuration File (appsettings.json)

```json
{
  "AppSettings": {
    "Folder": "C:\\temp\\videos",
    "Port": 5000
  }
}
```

## How It Works

1. The library starts a TCP listener on the specified port
2. When a client connects, it receives:
   - The file name length (as an integer)
   - The file name (as a UTF-8 string)
   - The expected SHA-256 checksum (32 bytes)
   - The file data
3. The file is saved to the specified folder
4. The actual checksum is calculated and compared with the expected checksum
5. The result is reported to the console

## Advanced Usage

### Error Handling

The library includes built-in error handling, but you can add additional error handling in your application:

```csharp
try
{
    string fileName = GetVideo.Download(downloadFolder, port);
    // Process the downloaded file
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Handle the error
}
```

## License

This project is licensed under the Creative Commons CC0-1.0 Universal License - see the [LICENSE](LICENSE) file for details.

CC0-1.0 is a public domain dedication that allows you to freely use, modify, and distribute this code without any restrictions.
