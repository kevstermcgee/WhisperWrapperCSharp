using WhisperWrapperCSharp;

/// <summary>
/// This is a program that creates subtitle files for video files using whisper.cpp
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        // Get the location of tools
        var exeLocation = AppContext.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(exeLocation, "..\\..\\..\\"));
        var ffmpegWindows = Path.Combine(projectRoot, "runtimes", "windows", "ffmpeg", "ffmpeg.exe");
        var whisperWindows = Path.Combine(projectRoot, "runtimes", "windows", "whisper", "whisper.cpp", "build", "bin", "release", "whisper-cli.exe");
        var whisperModelWindows = Path.Combine(projectRoot, "runtimes", "windows", "whisper", "whisper.cpp", "models", "ggml-large-v2.bin");

        // Empty list for video files
        List<string> filepaths = new List<string>();
        List<string> filepathsSuccess = new List<string>();
        List<string> filepathsFail = new List<string>();
        
        // Defines emtpy wav command variable
        string commandWav;

        // Filepath prompt
        Console.Write("Filepath here >> ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("No path provided. Closing...");
            Environment.Exit(1);
        }
        string filepath = input.Trim('\"');

        // Helper method to safely enumerate all files, skipping inaccessible folders
        static IEnumerable<string> SafeEnumerateFiles(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var currentDir = stack.Pop();
                string[] files = Array.Empty<string>();
                string[] subDirs = Array.Empty<string>();

                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException) { }
                catch (Exception) { }

                foreach (var file in files)
                    yield return file;

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (UnauthorizedAccessException) { }
                catch (Exception) { }

                foreach (var dir in subDirs)
                    stack.Push(dir);
            }
        }

        // Process filepath
        if (Directory.Exists(filepath))
        {
            foreach (var file in SafeEnumerateFiles(filepath))
            {
                filepaths.Add(file); // Adds the file to the filepaths list
            }
        }
        else if (File.Exists(filepath)) { 
            filepaths.Add(filepath); // Adds file to filepaths list
        }

        try
        {
            foreach (var file in filepaths)
            {
                // Create a temporary folder        
                var tempFolder = Path.Combine(Path.GetTempPath(), "whisper_wrapper_csharp_temp_" + Guid.NewGuid());
                Directory.CreateDirectory(tempFolder);

                // File extension
                var ext = Path.GetExtension(file).TrimStart('.');

                // Filepath without extension
                var filepathWithoutExt = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, Path.GetFileNameWithoutExtension(file));

                // Temporary files
                var tempInput = Path.Combine(tempFolder + "\\input." + ext);
                var tempOutput = Path.Combine(tempFolder + "\\output");
                var tempOutputSrt = Path.Combine(tempFolder + "\\output.srt");

                // Copy to the temporary folder as an input file
                File.Copy(file, tempInput);

                // Run the FFMPEG "ToWAV" command
                commandWav = ConvertCommand.ToWAV(tempInput, tempOutput);
                Console.WriteLine(commandWav);
                RunTerminalCommand(ffmpegWindows, commandWav);

                // Run the whisper command
                var commandSrt = ConvertCommand.ToSrt(whisperModelWindows, tempOutput);
                Console.WriteLine(commandSrt);
                RunTerminalCommand(whisperWindows, commandSrt);

                // Move subtitles back to the original directory
                File.Move(tempOutputSrt, filepathWithoutExt + ".srt");
                Directory.Delete(tempFolder, true);

                // Add filepath to the success list
                filepathsSuccess.Add(file);
            }

            // Completion message 
            Console.WriteLine($"Subtitles were created for: ");
            foreach (var file in filepathsSuccess) 
            {
                Console.WriteLine(file); // List successfully created subtitles
            }
            if (filepathsFail.Count > 0)
            {
                Console.WriteLine("There was a problem generating subtitles for these files: ");
                foreach (var file in filepathsFail)
                {
                    Console.WriteLine(file);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine("Unauthorized to access: " + ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }

        // Keeps window open
        Console.WriteLine("Press any button to close window...");
        Console.ReadLine();
    }

    /// <summary>
    /// This method is specifically for running commands.
    /// </summary>
    /// <param name="tool">The tool being used (ffmpeg or whisper)</param>
    /// <param name="command">The command generated by ConvertCommand.cs</param>
    public static void RunTerminalCommand(string tool, string command) 
    {
        CommandRunner.RunProgram(tool + command);
    }
}