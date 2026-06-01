using System.Text.Json;
using ResetFlow.Core.Models;
using ResetFlow.Core.SystemServices;

namespace ResetFlow.Core.Logging;

public sealed class RunLogger
{
    private readonly string _textLogPath;
    private readonly List<string> _lines = [];
    private readonly List<CommandResult> _commands = [];

    public RunLogger(string textLogPath)
    {
        _textLogPath = textLogPath;
    }

    public string TextLogPath => _textLogPath;
    public IReadOnlyList<string> Lines => _lines;
    public IReadOnlyList<CommandResult> Commands => _commands;

    public void Info(string message) => Write("INFO", message);
    public void Error(string message, Exception? exception = null) =>
        Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    public void Command(CommandResult result)
    {
        _commands.Add(result);
        Write("COMMAND", $"{result.FileName} {result.Arguments} => {result.ExitCode}");
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            Write("STDOUT", result.StandardOutput.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            Write("STDERR", result.StandardError.Trim());
        }
    }

    public async Task FlushTextAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_textLogPath)!);
        await File.WriteAllLinesAsync(_textLogPath, _lines, cancellationToken);
    }

    public async Task WriteJsonSummaryAsync(string jsonPath, ExecutionSummary summary, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        var payload = new
        {
            summary,
            commands = _commands,
            logLines = _lines
        };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(payload, JsonOptions), cancellationToken);
    }

    private void Write(string level, string message)
    {
        _lines.Add($"[{DateTimeOffset.Now:O}] [{level}] {message}");
    }

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
