using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Meeting_Transcriber_summariser
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
            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -i \"{_sourcePath}\" -vn -ac 1 -ar 16000 " + $"-c:a pcm_s16le \"{_extractedWavPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            ffmpegProcess.Start();

            string standardOutput = ffmpegProcess.StandardOutput.ReadToEnd();
            string standardError = ffmpegProcess.StandardError.ReadToEnd();

            ffmpegProcess.WaitForExit();


            if (ffmpegProcess.ExitCode != 0)
            {
                throw new Exception(
                    $"FFmpeg failed with exit code {ffmpegProcess.ExitCode}.{Environment.NewLine}" +
                    $"Error: {standardError}{Environment.NewLine}" +
                    $"Output: {standardOutput}");
            }
        }


    }
}
