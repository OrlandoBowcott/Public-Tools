using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Meeting_Transcriber___summariser
{
    internal class Audio
    {
        public readonly string _sourcePath;
        public readonly string _name;
        public readonly string _extractedWavPath;


        public Audio(string sourcePath)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Source media file not found.", sourcePath);
            _sourcePath = sourcePath;
            _name = Path.GetFileNameWithoutExtension(sourcePath);
            _extractedWavPath = Path.Combine($"{Path.GetDirectoryName(sourcePath)}", $"{Path.GetFileNameWithoutExtension(sourcePath)}", "processed.wav");
        }

        public void ExtractWAV()
        {
            string outputDirectory = Path.GetDirectoryName(_extractedWavPath)!;
            Directory.CreateDirectory(outputDirectory);

            double totalDurationSeconds = ProbeDurationSeconds(_sourcePath);

            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -i \"{_sourcePath}\" -vn -ac 1 -ar 16000 " +
                                $"-c:a pcm_s16le -progress pipe:1 -nostats \"{_extractedWavPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var stderrBuffer = new StringBuilder();

            ffmpegProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null) return;

                if (e.Data.StartsWith("out_time_ms="))
                {
                    if (long.TryParse(e.Data.Split('=')[1], out long microseconds))
                    {
                        double currentSeconds = microseconds / 1_000_000.0;
                        double percent = Math.Min(100, currentSeconds / totalDurationSeconds * 100);
                        PrintProgressBar(percent);
                    }
                }
                else if (e.Data == "progress=end")
                {
                    PrintProgressBar(100);
                    Console.WriteLine(" Done.");
                }
            };

            ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null) stderrBuffer.AppendLine(e.Data);
            };

            ffmpegProcess.Start();
            ffmpegProcess.BeginOutputReadLine();
            ffmpegProcess.BeginErrorReadLine();

            ffmpegProcess.WaitForExit();

            if (ffmpegProcess.ExitCode != 0)
            {
                throw new Exception(
                    $"FFmpeg failed with exit code {ffmpegProcess.ExitCode}.{Environment.NewLine}" +
                    $"Error: {stderrBuffer}");
            }
        }

        private static void PrintProgressBar(double percent)
        {
            const int barWidth = 30;
            int filled = (int)(barWidth * percent / 100);
            string bar = new string('#', filled) + new string('-', barWidth - filled);
            Console.Write($"\r[{bar}] {percent,5:F1}%");
        }

        private static double ProbeDurationSeconds(string path)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of csv=p=0 \"{path}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi)!;
            string output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return double.Parse(output, CultureInfo.InvariantCulture);
        }


    }
}
