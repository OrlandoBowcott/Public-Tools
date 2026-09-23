using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Meeting_Transcriber_summariser
{
    internal class Audio
    {
        private string SourcePath;
        private string extractedWAVPath;
        private string Name;


        public Audio(string sourcePath)
        {
            SourcePath = sourcePath;
            Name = Path.GetFileName(sourcePath);
            extractedWAVPath = Path.Combine($"{Path.GetDirectoryName(sourcePath)}", $"{Path.GetFileNameWithoutExtension(sourcePath)}", "processed.wav");
        }

        public void ExtractWAV()
        {
            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{SourcePath}\" -vn -ac 1 -ar 16000 " + $"-c:a pcm_s16le \"{extractedWAVPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            ffmpegProcess.Start();
            ffmpegProcess.WaitForExit();
            if (ffmpegProcess.ExitCode != 0)
            {
                string errorMessage = ffmpegProcess.StandardError.ReadToEnd();
                throw new Exception($"FFmpeg failed with exit code {ffmpegProcess.ExitCode}: {errorMessage}");
            }
            File.WriteAllBytes(extractedWAVPath, File.ReadAllBytes(extractedWAVPath));
        }
    }
}
