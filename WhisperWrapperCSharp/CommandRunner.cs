using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WhisperWrapperCSharp;
/// <summary>
/// This class is specifically for running commands.
/// </summary>
public class CommandRunner
{
    public static void RunProgram(string command)
    {
        string shell;
        string arguments;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            shell = "cmd.exe";
            arguments = $"/c \"{command}\"";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            shell = "/bin/bash";
            arguments = $"-c \"{command}\"";
        } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            shell = "/bin/bash";
            arguments = $"-c \"{command}\"";
        }
        else
        {
            throw new Exception("Unsupported OS");
        }
        
        RunCommand(shell, arguments);
    }

    private static int RunCommand(string shell, string arguments)
    {
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.OutputDataReceived += (sender, outputArgs) =>
        {
            if (outputArgs.Data != null) Console.WriteLine($"OUT: {outputArgs.Data}");
        };
        
        process.ErrorDataReceived += (sender, errorArgs) =>
        {
            if (errorArgs.Data != null) Console.WriteLine(errorArgs.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;
    }
}