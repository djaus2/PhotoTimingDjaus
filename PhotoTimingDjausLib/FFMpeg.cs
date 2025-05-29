using FFMpegCore;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Runtime.Versioning;
using FFMpegCore.Arguments;

namespace PhotoTimingDjausLib
{
    public static class FFMpegActions
    {
        
        public static int numAudioFrames = 0;

        /*
        public static void ParseSoundperFrame()
        {
            Process process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = "-i video.mp4 -filter:a astats=metadata=1:reset=1 -f null -";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Console.WriteLine($"Audio Stats:\n{output}");
        }
        public static async Task Truncate(string inputPath, string outputPath, TimeSpan startTime, int duration = 0)
        {
            var mediaInfo = FFProbe.Analyse(inputPath); // Use FFProbe to get media info  
            var videoduration = mediaInfo.Duration;
            TimeSpan _duration = TimeSpan.FromSeconds(duration);

            if (duration > 0)
            {
                var availableDuration = videoduration - startTime;
                if (availableDuration.TotalSeconds < duration)
                {
                    throw new ArgumentException($"Duration {duration} exceeds available video duration {availableDuration.TotalSeconds} seconds after start time {startTime.TotalSeconds} seconds.");
                }
            }
            else
            {
                TimeSpan availableDuration = videoduration - startTime;
                if (availableDuration.TotalSeconds < 0)
                {
                    throw new ArgumentException($"StartTime {startTime} exceeds available video duration {availableDuration.TotalSeconds}");
                }
                _duration = availableDuration;
            }
            System.Diagnostics.Debug.WriteLine($"Video Length {videoduration}");
            System.Diagnostics.Debug.WriteLine($"Start time {startTime}");
            System.Diagnostics.Debug.WriteLine($"New Video Length {_duration}");
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
                System.Diagnostics.Debug.WriteLine($"Existing file at '{outputPath}' has been deleted.");
            }
            // Replace the incorrect usage of FFMpeg.Conversions with the correct method call
            await FFMpeg.SubVideoAsync(inputPath, outputPath, startTime, videoduration);
        }

        public static void getAudio(string videoPath, string audioPath)
        {
            string tempPath = @"C:\Users\david\OneDrive\Documents\Downloads\ffmpeg-master-latest-win64-gpl-shared\ffmpeg-master-latest-win64-gpl-shared\bin";
            string? currentPath = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrEmpty(currentPath))
            {
                if (!currentPath.Split(';').Contains(tempPath))
                {
                    Environment.SetEnvironmentVariable("PATH", currentPath + ";" + tempPath);
                    FFMpegArguments
                    .FromFileInput(videoPath)
                    .OutputToFile(audioPath, true, options => options.WithAudioCodec("pcm_s16le"))
                    .ProcessSynchronously();
                }
            }
        }

        //public static string GetPeekandMeans(string audioFile)
        //{
        //    //string output = FFMpegArguments
        //    //    .FromFileInput("C:\temp\vid\audio.wav")
        //    //    .OutputToFile(@"c:\temp\peekandmeans.txt", true, static options => options
        //    //        .WithCustomArgument("-filter:a volumedetect")
        //    //        .WithCustomArgument("-f null"));



        //    System.Diagnostics.Debug.WriteLine($"Peek and Means Output: {output}");
        //    return output;
        //}

        
        public static TimeSpan GetGunTime(string output)
        {
            string timestamp = "";
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                if (line.Contains("max_volume"))
                {
                    timestamp = Regex.Match(line, @"pts_time:(\d+\.\d+)").Groups[1].Value;
                    System.Diagnostics.Debug.WriteLine($"Start Gun Detected at: {timestamp} seconds");
                    break;
                }
            }
            if (string.IsNullOrEmpty(timestamp))
            {
                throw new Exception("Gun time not found in the output.");
            }
            return TimeSpan.FromSeconds(double.Parse(timestamp));
        }

        //public static TimeSpan  GetGunTimeofStart(string inputPath, string outputPath)
        //{
        //    Truncate(inputPath, outputPath, TimeSpan.FromSeconds(0), 10);
        //    getAudio(outputPath);
        //    string output = GetPeekandMeans();
        //    TimeSpan gunTime = FFMpegActions.GetGunTime(output);
        //    System.Diagnostics.Debug.WriteLine($"Gun time detected at: {gunTime.TotalSeconds} seconds");
        //    return gunTime;
        //}
        

        
        /// <summary>
        /// Get peak guntime from (wave) audio file
        /// Uses WaveFileReader from NuGet NAudio package
        /// </summary>
        /// <param name="filePath">The audio file</param>
        /// <returns></returns>
        public static TimeSpan GetPeakTimestamp(string filePath)
        {
            using (var reader = new WaveFileReader(filePath))
            {
                float maxPeak = 0;
                long peakSampleIndex = 0;
                int sampleRate = reader.WaveFormat.SampleRate;
                int bytesPerSample = reader.WaveFormat.BitsPerSample / 8;

                byte[] buffer = new byte[reader.WaveFormat.BlockAlign];
                long sampleIndex = 0;

                while (reader.Read(buffer, 0, buffer.Length) > 0)
                {
                    float sampleValue = BitConverter.ToSingle(buffer, 0);
                    if (Math.Abs(sampleValue) > maxPeak)
                    {
                        maxPeak = Math.Abs(sampleValue);
                        peakSampleIndex = sampleIndex;
                    }
                    sampleIndex++;
                }

                return TimeSpan.FromSeconds((double)peakSampleIndex / sampleRate);
            }
        }

        
        /// <summary>
        /// Truncate video file from when guyn is detected
        /// </summary>
        /// <param name="videoPath">Video file</param>
        /// <param name="audioPath">Audio file created from video</param>
        /// <param name="outputPath">The truncated file</param>
        /// <returns>The gun time</returns>
        public static async Task<TimeSpan> GetGunTimeofStart(string videoPath, string audioPath, string outputPath)
        {
            getAudio(videoPath, audioPath);
            TimeSpan gunTime = GetPeakTimestamp(audioPath);
            await Truncate(videoPath, outputPath, gunTime);
            System.Diagnostics.Debug.WriteLine($"Gun time detected at: {gunTime.TotalSeconds} seconds");

            return gunTime;
        }
        */

        /// <summary>
        /// Get raw audio info from video file
        /// </summary>
        /// <param name="inputPath">video file</param>
        /// <param name="outputPath">raw audio data file</param>
        static void GetGunLoudness(string inputPath, string outputPath)
        {
            string tempPath = @"C:\Users\david\OneDrive\Documents\Downloads\ffmpeg-master-latest-win64-gpl-shared\ffmpeg-master-latest-win64-gpl-shared\bin";
            string? currentPath = Environment.GetEnvironmentVariable("PATH");

            if (!string.IsNullOrEmpty(currentPath))
            {
                if (!currentPath.Split(';').Contains(tempPath))
                {
                    string arguments = $"-i \"{inputPath}\" -filter_complex  \"[0:a]astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level\"  -f null  nul 2 > \"{outputPath}\" ";
                    System.Diagnostics.Debug.WriteLine($"FFMpeg Arguments: {arguments}");

                    Environment.SetEnvironmentVariable("PATH", currentPath + ";" + tempPath);
                }
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{inputPath}\" -filter_complex \"[0:a]astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level\" -f null nul",
                        RedirectStandardError = true, // Redirect stderr
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                // Read stderr and save it to a file
                string stderr = process.StandardError.ReadToEnd();
                File.WriteAllText(outputPath, stderr);

                process.WaitForExit();

            }
        }


        /// <summary>
        /// Get peak volume of sound frames against sound (not video) frame index and progressive time.
        /// </summary>
        /// <param name="videoPath">The video capture file</param>
        /// <param name="guninfoPath">The structured sound frame number, PTS, frametime, and max volue file</param>
        /// <exception cref="ArgumentException"></exception>
        public static void Filterdata(string videoPath, string guninfoPath)
        {

            if (string.IsNullOrEmpty(videoPath))
            {
                throw new ArgumentException("Video path cannot be null or empty.", nameof(videoPath));
            }
            if (!string.IsNullOrEmpty(guninfoPath))
            {
                File.Delete(guninfoPath);
            }
            string fileName = Path.GetFileName(guninfoPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(guninfoPath);
            string fileExtension = Path.GetExtension(guninfoPath);
            string? folderPath = Path.GetDirectoryName(guninfoPath);
            string guninfoPathRaw = $"{folderPath}\\{fileNameWithoutExtension}RAW.{fileExtension}";
            string guninfoPathRawFiltered = $"{folderPath}\\{fileNameWithoutExtension}FILTEREDRAW.{fileExtension}";

            GetGunLoudness(videoPath, guninfoPathRaw);
            // Read all lines from the file
            var lines = File.ReadAllLines(guninfoPathRaw);

            // Filter lines containing "Parsed_ametadata_1"
            var filteredLines = lines.Where(line => line.Contains("[Parsed_ametadata_1 @")).ToList();

            // Write filtered lines to a new file
            File.WriteAllLines(guninfoPathRawFiltered, filteredLines);
            List<string> filteredLines2 = new List<string> { "Frame,PTS,PTS_Time,Volume" };
            for (int i = 0; i < filteredLines.Count;)
            {
                int frameNumber = 0;
                int pts = 0;
                double pts_time = 0.0;
                double volume = 0.0;
                ////////////////////////////////

                string line1 = filteredLines[i];
                // Check if line contains "frame:"
                if (!line1.Contains("frame:"))
                {
                    //skip
                    i++;
                    continue;
                }
                if (!line1.Contains("pts_time"))
                {
                    //skip
                    i++;
                    continue;
                }
                i++;

                string[] parts = line1.Split( ' ',StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {

                    if (parts[3].StartsWith("frame:"))
                    {
                        parts[3] = parts[3].Substring("frame:".Length); // Remove "frame:" prefix
                    }
                    else
                        continue;
                    if (parts[4].StartsWith("pts:"))
                    {
                        parts[4] = parts[4].Substring("pts:".Length); // Remove "frame:" prefix
                    }
                    else
                        continue;
                    if (parts[5].StartsWith("pts_time:"))
                    {
                        parts[5] = parts[5].Substring("pts_time:".Length); // Remove "frame:" prefix
                    }
                    else
                        continue;
                    string frameNumberStr = parts[3].Trim();
                    string ptsStr = parts[4].Trim();
                    string ptsTimeStr = parts[5].Trim();
                    if (int.TryParse(frameNumberStr, out frameNumber) &&
                        int.TryParse(ptsStr, out pts) &&
                        double.TryParse(ptsTimeStr, out pts_time))
                    {
                        // Successfully parsed frame number, pts, and pts_time
                    }
                    else
                    {
                        // Parsing failed, skip this line
                        continue;
                    }
                }

                string line2 = filteredLines[i];
                i++;
                // Check if line contains "lavfi.astats.Overall.RMS_level"
                if (!line2.Contains("lavfi.astats.Overall.RMS_level"))
                {
                    //skip
                    continue;
                }
                parts = line2.Split(' ',StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 4)
                    continue;
                string data = parts[3];
                string[] info = data.Split('=');
                if (info.Length < 2)
                    continue;
                if (info[0] != "lavfi.astats.Overall.RMS_level")
                    continue;
                if (double.TryParse(info[1], out double rmsLevel))
                {
                    volume = rmsLevel;
                }
                else
                    continue;
                
                filteredLines2.Add($"{frameNumber},{pts},{pts_time},{volume}");
            }
            numAudioFrames = filteredLines2.Count();
            File.WriteAllLines(guninfoPath, filteredLines2);
            Console.WriteLine($"Filtered data saved to: {guninfoPath}");
           
        }
    }

}

