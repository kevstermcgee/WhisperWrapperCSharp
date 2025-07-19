using System;
using System.IO;
using System.Runtime.CompilerServices;
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
        var whisperModelWindows = Path.Combine(projectRoot, "runtimes", "windows", "whisper", "whisper.cpp", "models", "ggml-base.en.bin");

        // Empty list for video files
        List<string> filepaths = new List<string>();
        List<string> filepathsSuccess = new List<string>();
        List<string> filepathsFail = new List<string>();
        List<string> tempFolders = new List<string>();

        // Fires when the program is exiting normally or being killed
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            Console.WriteLine("Process is exiting. Cleaning up...");
            RunCleanupCommand(tempFolders);
        };

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Program interrupted (Ctrl+C or close). Running cleanup...");
            RunCleanupCommand(tempFolders);

            // Prevent immediate termination
            e.Cancel = true;
        };

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
                filepaths.Add(file); // Adds file to the filepaths list
            }
        }
        else if (File.Exists(filepath)) { // Adds file to filepaths list
            filepaths.Add(filepath);
        }

        try
        {
            foreach (var file in filepaths)
            {
                
                while (true)
                {
                    // Create a temporary folder        
                    var tempFolder = Path.Combine(Path.GetTempPath(), "whisper_wrapper_csharp_temp_" + Guid.NewGuid());
                    Directory.CreateDirectory(tempFolder);

                    // Add temporary folder location to tempFolders list
                    tempFolders.Add(tempFolder);

                    // File extension
                    var ext = Path.GetExtension(file).TrimStart('.');

                    // Filepath without extension
                    var filepathWithoutExt = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, Path.GetFileNameWithoutExtension(file));

                    // Temporary files
                    var tempInput = tempFolder + "\\input." + ext;
                    var tempOutput = tempFolder + "\\output";
                    var tempOutputSrt = tempFolder + "\\output.srt";

                    // Copy to the temporary folder as an input file
                    File.Copy(file, tempInput);

                    // Run the FFMPEG "ToWAV" command
                    var commandWAV = ConvertCommand.ToWAV(tempInput, tempOutput + ".wav");
                    RunTerminalCommand(ffmpegWindows, commandWAV);

                    // Run the whisper command
                    var commandSRT = ConvertCommand.ToSrt(whisperModelWindows, tempOutput + ".wav", tempOutput);
                    RunTerminalCommand(whisperWindows, commandSRT);

                    // Move subtitles back to original directory
                    File.Move(tempOutputSrt, filepathWithoutExt + ".srt");

                    // Add filepath to success list
                    filepathsSuccess.Add(file);
                }
            }

            // Completion message 
            Console.WriteLine($"Subtitles were created for: ");
            foreach (var file in filepathsSuccess) // List successfully created subtitles
            {
                Console.WriteLine(file);
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

        // Clean up at the end. Remove temporary folders.
        RunCleanupCommand(tempFolders);

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

    public static void RunCleanupCommand(List<string> tempFolders)
    {
        // Delete all temporary folders
        foreach (string folder in tempFolders)
        {
            Directory.Delete(folder, true);
        }
    }
}