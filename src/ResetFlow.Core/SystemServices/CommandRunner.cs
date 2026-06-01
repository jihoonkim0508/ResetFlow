using System.Diagnostics;

namespace ResetFlow.Core.SystemServices;

public sealed record CommandResult(
    string FileName,
    string Arguments,
    int ExitCode,
    string StandardOutput,
    string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken);
}

public sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<CommandResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return new CommandResult(fileName, arguments, process.ExitCode, await outputTask, await errorTask);
    }
}
