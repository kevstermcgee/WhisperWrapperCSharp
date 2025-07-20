namespace WhisperWrapperCSharp;
/// <summary>
/// This class is for generating commands for ffmpeg and whisper.
/// </summary>
public class ConvertCommand
{
    public static string ToWAV(string inputFile, string outputFile)
    {
        // return $" -i \"{inputFile}\" \"{outputFile + ".wav"}\"";
        return $" -i \"{inputFile}\" -vn -acodec pcm_s16le -ar 44100 -ac 2 \"{outputFile + ".wav"}\"";
    }

    public static string ToSrt(string model, string outputFile)
    {
        return $" -m \"{model}\" -l en -f \"{outputFile}.wav\" -osrt -of \"{outputFile}\"";
    }
}