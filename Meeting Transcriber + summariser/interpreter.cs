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
        private record TimedToken(
            TimeSpan Start,
            TimeSpan End,
            string Text
        );

        public Transcriber(string modelPath, GgmlType modelType = GgmlType.LargeV3)
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

            var timedTokens = new List<TimedToken>();

            using var whisperFactory = WhisperFactory.FromPath(_modelPath);
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("en")
                .WithTokenTimestamps()
                .WithProgressHandler(progress =>
                {
                    Console.Write($"\rTranscribing: {progress,3}%   ");
                })
                .Build();

            using var fileStream = File.OpenRead(wavPath);

            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                foreach (var token in result.Tokens)
                {
                    if (string.IsNullOrEmpty(token.Text))
                        continue;

                    // Ignore Whisper control tokens such as <|endoftext|>.
                    if (token.Text.StartsWith("<|"))
                        continue;

                    TimeSpan tokenStart = token.Start >= 0
                        ? TimeSpan.FromMilliseconds(token.Start * 10.0)
                        : result.Start;

                    TimeSpan tokenEnd = token.End >= token.Start
                        ? TimeSpan.FromMilliseconds(token.End * 10.0)
                        : result.End;

                    timedTokens.Add(
                        new TimedToken(tokenStart, tokenEnd, token.Text)
                    );
                }
            }

            var segments = BuildSentenceSegments(timedTokens);

            Console.WriteLine("\rTranscribing: 100%   Done.");

            string transcriptPath = Path.ChangeExtension(wavPath, ".txt");
            using (var writer = new StreamWriter(transcriptPath))
            {
                foreach (var segment in segments)
                {
                    writer.WriteLine($"[{segment.Start:hh\\:mm\\:ss} - {segment.End:hh\\:mm\\:ss}] {segment.Text}");
                }
            }
            Console.WriteLine($"Transcript saved to: {transcriptPath}");

            return segments;
        }

        private static List<TranscriptSegment> BuildSentenceSegments(
    List<TimedToken> tokens)
        {
            var sentences = new List<TranscriptSegment>();
            var textBuffer = new StringBuilder();

            TimeSpan? sentenceStart = null;
            TimeSpan sentenceEnd = TimeSpan.Zero;
            TimeSpan previousTokenEnd = TimeSpan.Zero;

            void FinishSentence()
            {
                string sentenceText = textBuffer.ToString().Trim();

                if (sentenceText.Length > 0 && sentenceStart.HasValue)
                {
                    sentences.Add(new TranscriptSegment(
                        sentenceStart.Value,
                        sentenceEnd,
                        sentenceText
                    ));
                }

                textBuffer.Clear();
                sentenceStart = null;
            }

            foreach (TimedToken token in tokens)
            {
                // Treat a long pause as a boundary even if Whisper added no punctuation.
                if (sentenceStart.HasValue &&
                    token.Start - previousTokenEnd > TimeSpan.FromMilliseconds(900))
                {
                    FinishSentence();
                }

                sentenceStart ??= token.Start;

                textBuffer.Append(token.Text);
                sentenceEnd = token.End;
                previousTokenEnd = token.End;

                if (EndsSentence(textBuffer))
                {
                    FinishSentence();
                }
            }

            // Save any unfinished sentence at the end of the recording.
            FinishSentence();

            return sentences;
        }

        private static bool EndsSentence(StringBuilder text)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                char character = text[i];

                if (char.IsWhiteSpace(character) ||
                    character is '"' or '\'' or ')' or ']' or '}' or '”' or '’')
                {
                    continue;
                }

                return character is '.' or '?' or '!';
            }

            return false;
        }
    }
}
