using FFMpegCore;
using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Android.Provider.MediaStore;
using NAudio.Wave;
using Java.Util;
using static Android.Renderscripts.ScriptGroup;

namespace DetectAudioFlash
{
    public static class FFMpegActions
    {

        /// <summary>
        /// Write the gun time as metadata (comment) to the video file.
        /// </summary>
        /// <param name="videoFilePath">Video File Path</param>
        /// <param name="gunTime">Gun Time before or after video start as TimeSpan</param>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void WriteGunTime(string videoFilePath, TimeSpan gunTime)
        {
            string inputPath = "videoFilePath";
            if (!File.Exists(inputPath))
            {
                throw new FileNotFoundException($"The specified video file does not exist: {inputPath}");
            }
            string extension = Path.GetExtension(inputPath);
            if (string.IsNullOrEmpty(extension) || !extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The input file must be an MP4 video file.");
            }
            string outputPath = inputPath.Replace(".mp4", "_startset.mp4",StringComparison.OrdinalIgnoreCase);
            string gunTimeString = gunTime.ToString(@"hh\:mm\:ss\.fff");
            FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithCustomArgument("-metadata comment={gunTimeString}")
                    .WithCustomArgument("-codec copy"))
                .ProcessSynchronously();
        }
        public static void Truncate(string inputPath, string outputPath, TimeSpan startTime, int duration=0 )
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
            FFMpeg.SubVideo(inputPath,
                outputPath,
                startTime,
                _duration);
        }

        public static void getAudio(string outputPath)
        {
            FFMpegArguments
            .FromFileInput(outputPath)
            .OutputToFile("audio.wav", false, options => options.WithAudioCodec("pcm_s16le"))
            .ProcessSynchronously();
        }



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


        public static TimeSpan  GetGunTimeofStart(string inputPath, string outputPath)
        {
            getAudio(outputPath);
            TimeSpan gunTime = GetPeakTimestamp(outputPath);
            Truncate(inputPath, outputPath, gunTime);
            Console.WriteLine($"Gun time detected at: {gunTime.TotalSeconds} seconds");
            return gunTime;
        }

    }
}
