
namespace Meeting_Transcriber_summariser
{
    class Program
    {
        static void Main(string[] args)
        { 
            Console.WriteLine("please paste the exact file path of the audio file you want to transcribe and summarise: ");
            string audioFilePath = Console.ReadLine();
            Audio audioData = new Audio(audioFilePath);
            audioData.ExtractWAV();

        }
    }
}