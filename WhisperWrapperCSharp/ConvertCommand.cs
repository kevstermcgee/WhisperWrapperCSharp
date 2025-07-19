namespace WhisperWrapperCSharp;

public class ConvertCommand
{
    public static string ToWAV(string inputFile, string outputFile)
    {
        return $" -i \"{inputFile}\" \"{outputFile}\"";
    }

    public static string ToSrt(string model, string outputFile, string outputFileSrt)
    {
        return $" -m {model} -l en -f \"{outputFile}\" -osrt -of \"{outputFileSrt}\"";
    }
}