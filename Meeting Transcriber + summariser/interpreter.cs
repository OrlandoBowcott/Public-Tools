using System;
using System.Collections.Generic;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;

namespace Meeting_Transcriber___summariser
{

    public record TranscriptSegment(TimeSpan Start, TimeSpan End, string Text);

    internal class Transcriber
    {
        private readonly string _modelPath;
        private readonly GgmlType _modelType;

        public Transcriber(string modelPath, GgmlType modelType = GgmlType.Medium)
        {
            _modelPath = modelPath;
            _modelType = modelType;
        }

        public async Task EnsureModelDownloadedAsync()
        {
            if (File.Exists(_modelPath))
                return;

            using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(_modelType);
            using var fileWriter = File.OpenWrite(_modelPath);
            await modelStream.CopyToAsync(fileWriter);
        }

        public async Task<List<TranscriptSegment>> TranscribeAsync(string wavPath)
        {
            await EnsureModelDownloadedAsync();

            var segments = new List<TranscriptSegment>();

            using var whisperFactory = WhisperFactory.FromPath(_modelPath);
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("en")
                .WithProgressHandler(progress =>
                {
                    Console.Write($"\rTranscribing: {progress,3}%   ");
                })
                .Build();

            using var fileStream = File.OpenRead(wavPath);

            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                segments.Add(new TranscriptSegment(result.Start, result.End, result.Text));
            }

            Console.WriteLine("\rTranscribing: 100%   Done.");
            return segments;
        }
    }
}
