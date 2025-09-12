using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;

    /*--This project no longer used.
  
    See Nuget package/s Sportronics.VideoEnums.Windows and Sportronics.VideoEnums (Android version)
    https://www.nuget.org/packages/Sportronics.VideoEnums.Windows/ 
    https://www.nuget.org/packages/Sportronics.VideoEnums/ 

    Sources:
    https://github.com/djaus2/TransferVideoOverTcp/blob/master/VideoEnums.Windows/TimeFromMode.cs
    https://github.com/djaus2/TransferVideoOverTcp/blob/master/VideoEnums/TimeFromMode.cs
    */

namespace PhotoTimingDjaus.Enum.Local
{
    public enum TimeFromMode
    {
        FromVideoStart, //From start of video capture
        FromGunSound, //From gun sound
        FromGunFlash,  //From observed flash of gun on video
        ManuallySelect, //Manually selected start time
        WallClockSelect
    }

    public enum VideoDetectMode
    {
        FromFlash, //Detect flash in video
        FromFrameChange, //Detect motion in video
        FromMotionDetector //Detect frame change in video.
    }

    public class VideoInfo
    {
        public VideoInfo()
        {

        }


        public VideoInfo(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            json = json.Trim();
            if (!(json.StartsWith("{") && json.EndsWith("}")) &&  // Object
                !(json.StartsWith("[") && json.EndsWith("]")))    // Array
            {
                string prefixedFilename = json;
                SelectedFilename = prefixedFilename;
                GetMetaInfoandFilename();
                return;
            }
            else
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                }
                catch
                {
                    return;
                }
                var obj = JsonSerializer.Deserialize<VideoInfo>(json);
                if (obj != null)
                {
                    RecordedVideoPath = "";
                    SelectedVideoPath = "";
                    SelectedFilename = "";
                    VideoStart = obj.VideoStart;
                    GunTime = obj.GunTime;
                    DetectMode = obj.DetectMode;
                    TimeFrom = obj.TimeFrom;
                    Checksum = obj.Checksum;
                    RecordedFilename = obj.RecordedFilename;
                }
            }
        }

        public string RecordedFilename { get; set; }

        public string RecordedVideoPath { get; set; }

        public string SelectedVideoPath { get; set; }

        public string SelectedFilename { get; set; }

        public DateTime VideoStart { get; set; }
        public DateTime GunTime { get; set; }
        public VideoDetectMode DetectMode { get; set; }
        public TimeFromMode TimeFrom { get; set; }

        public byte[] Checksum { get; set; }




        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Encode metainfo as appendage to Filename
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static string EncodeDateTime(DateTime dat)
        {
            // yyyy-MM-dd HH--mm:ss.fff
            string datStr = dat.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string result = datStr.Replace(":", "--");
            return $"{result}";
        }
        public string ConcatenateTimeFromandVideoTimes()
        {
            string result = "";
            string videoStartStr = EncodeDateTime(VideoStart);
            string gunTimeString = EncodeDateTime(GunTime);
            string timeFromStr = "_" + TimeFrom.ToString().ToUpper().Replace("SELECT", "", StringComparison.OrdinalIgnoreCase) + "_";
            if (TimeFrom == TimeFromMode.WallClockSelect)
            {
                result = $"{timeFromStr}{videoStartStr}_{gunTimeString}";
            }
            else
            {
                result = $"{timeFromStr}{videoStartStr}";
            }

            return result;
        }

        /// <summary>
        /// Where have only the recorded video path , append the time info and TimeFron mode  to the filename and copy to SelectedVideoPath
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void AppendTimesToVideoFilename()
        {
            string extension = RecordedVideoPath.Substring(RecordedVideoPath.Length - 4, 4); // Get the last 4 characters for extension check
            if ((string.IsNullOrEmpty(extension)) ||
                (!extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("The input file must be an MP4 video file.");
            }
            string metaInfo = ConcatenateTimeFromandVideoTimes() + ".mp4";
            SelectedVideoPath = SelectedVideoPath.Replace(".mp4", metaInfo, StringComparison.OrdinalIgnoreCase);
            SelectedFilename = SelectedFilename.Replace(".mp4", metaInfo, StringComparison.OrdinalIgnoreCase);

#if WINDOWS
            try
            {
                // For Windows, we'll use a different approach to handle file operations
                string sourcePath = RecordedVideoPath;
                string destinationPath = SelectedVideoPath;

                // Use FileStream with sharing options to avoid file locking issues
                using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    sourceStream.CopyTo(destStream);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File access error: {ex.Message}");
                SelectedVideoPath = RecordedVideoPath;
            }
#else
            // Default implementation for other platforms
            try
            {
                string sourcePath = RecordedVideoPath;
                string destinationPath = SelectedVideoPath;

                // Ensure permissions are granted before this step!
                File.Copy(sourcePath, destinationPath, overwrite: true);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File access error: {ex.Message}");
                SelectedVideoPath = RecordedVideoPath;
            }
#endif
        }


        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Get Info from PrefixedFilename
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ParseCustomDateTime(string input)
        {
            // Example input: "2024-09-08 12--34--56.789"
            // Replace double dashes with colons in the time part
            int timeStart = input.IndexOf(' ') + 1;
            string normalized = input.Substring(0, timeStart) +
                                input.Substring(timeStart).Replace("--", ":");

            // Now normalized is "2024-09-08 12:34:56.789"
            return DateTime.ParseExact(normalized, "yyyy-MM-dd HH:mm:ss.fff", null);
        }

        /// <summary>
        /// Construct meta info from selected video file's filename for transmission to WPF app (as json string) over TCP prior to transmission to service.
        /// If no meta info in filename then set defaults and save copy with the meta info appended to the filename. 
        ///  - Do not send video in that case.
        ///  - Can then be selected.
        /// </summary>
        /// <returns>False if no meta info in filename</returns>
        public bool GetMetaInfoandFilename()
        {
            VideoStart = DateTime.MinValue;
            GunTime = DateTime.MinValue;

            string pattern = @"_WALLCLOCK_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})_(\d{4}-\d{2}-\d{2} \d{2}--\d{2}--\d{2}\.\d{3})\.mp4$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var match = regex.Match(SelectedFilename);
            if (match.Success)
            {
                RecordedFilename = SelectedFilename.Substring(0, match.Index) + ".mp4";
                TimeFrom = TimeFromMode.WallClockSelect;
                string videoDateTimeStr = match.Groups[1].Value;
                VideoStart = ParseCustomDateTime(videoDateTimeStr);
                string gunTimeDateTimeStr = match.Groups[2].Value;
                GunTime = ParseCustomDateTime(gunTimeDateTimeStr);
            }
            else
            {
                pattern = @"_([a-z]+)_([\d]{4}-[\d]{2}-[\d]{2} [\d]{2}--[\d]{2}--[\d]{2}\.\d{3})";
                regex = new Regex(pattern, RegexOptions.IgnoreCase);

                match = regex.Match(SelectedFilename);
                if (match.Success)
                {
                    RecordedFilename = SelectedFilename.Substring(0, match.Index) + ".mp4";
                    string filenameModeStr = match.Groups[1].Value;      // "FROMVIDEOSTART" (case as in input)
                    foreach (TimeFromMode mode in System.Enum.GetValues(typeof(TimeFromMode)))
                    {
                        string modeStr = $"{mode.ToString()}".ToUpper();
                        modeStr = modeStr.Replace("Select", "", StringComparison.OrdinalIgnoreCase);
                        modeStr = $"_{modeStr}_";
                        if (filenameModeStr.Contains(modeStr))
                        {
                            this.TimeFrom = mode;
                        }
                    }
                    string videoStartStr = match.Groups[2].Value;  // "2024-09-08 12--34--56.789"
                    VideoStart = ParseCustomDateTime(videoStartStr);
                }
                else
                {
                    pattern = @"_([a-zA-Z]+)_";
                    regex = new Regex(pattern, RegexOptions.IgnoreCase);

                    match = regex.Match(SelectedFilename);
                    if (match.Success)
                    {
                        RecordedFilename = SelectedFilename.Substring(0, match.Index) + ".mp4";
                        string filenameModeStr = match.Groups[1].Value;      // "FROMVIDEOSTART" (case as in input)
                        foreach (TimeFromMode mode in System.Enum.GetValues(typeof(TimeFromMode)))
                        {
                            string modeStr = $"{mode.ToString()}".ToUpper();
                            modeStr = modeStr.Replace("Select", "", StringComparison.OrdinalIgnoreCase);
                            if (filenameModeStr.Contains(modeStr))
                            {
                                this.TimeFrom = mode;
                            }
                        }
                    }
                    else
                    {
                        // If we get here then the selected filename has no meta info so set defaults
                        // Requires Recorded video <- Selected Video
                        RecordedVideoPath = SelectedVideoPath;
                        RecordedFilename = SelectedFilename;
                        TimeFrom = TimeFromMode.FromVideoStart;
                        VideoStart = DateTime.Now;
                        GunTime = DateTime.MinValue;
                        AppendTimesToVideoFilename();
                        return false;
                    }
                }
            }
            RecordedVideoPath = Path.Combine(Path.GetDirectoryName(SelectedVideoPath), RecordedFilename);
            GetCheckSum();
            return true;
        }


        public override string ToString()
        {
            return ToJson();
        }

        public void GetCheckSum()
        {
            try
            {
                // Use the same file access approach for all platforms
                // with appropriate sharing options to avoid locking issues
                using (var fileStream = new FileStream(SelectedVideoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using var sha256 = SHA256.Create();
                    byte[] checksum = sha256.ComputeHash(fileStream);
                    Checksum = checksum;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error computing checksum: {ex.Message}");
                // Create an empty checksum if we can't access the file
                Checksum = new byte[32]; // SHA-256 produces a 32-byte hash
            }
        }
    }
}
