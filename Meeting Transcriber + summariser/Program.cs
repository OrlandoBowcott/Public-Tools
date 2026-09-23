using Whisper.net.Ggml;


namespace Meeting_Transcriber___summariser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("please paste the exact file path of the audio file you want to transcribe and summarise: ");
            string audioFilePath = Console.ReadLine();

            Audio audioData = new Audio(audioFilePath);
            audioData.ExtractWAV();

            Transcriber transcriber = new Transcriber("ggml-baseEn.bin", GgmlType.BaseEn);
            List<TranscriptSegment> segments = await transcriber.TranscribeAsync(audioData._extractedWavPath);

            foreach (var segment in segments)
            {
                Console.WriteLine($"[{segment.Start:hh\\:mm\\:ss} - {segment.End:hh\\:mm\\:ss}] {segment.Text}");
            }
        }
    }
}