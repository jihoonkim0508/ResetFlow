using System.Text.Json;
using ResetFlow.Core.Logging;
using ResetFlow.Core.Models;

namespace ResetFlow.Core.Tests;

public sealed class LoggerTests
{
    [Fact]
    public async Task WriteJsonSummaryAsync_CreatesSummaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "ResetFlowTests", Guid.NewGuid().ToString("N"));
        var logger = new RunLogger(Path.Combine(directory, "20260601_100000.log"));
        logger.Info("test line");
        var summary = new ExecutionSummary
        {
            StartedAt = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero),
            FinishedAt = new DateTimeOffset(2026, 6, 1, 10, 1, 0, TimeSpan.Zero),
            WindowsVersion = "Windows",
            IsAdministrator = true,
            DeviceType = DeviceType.Desktop,
            GpuInfo = "GPU",
            TextLogPath = logger.TextLogPath
        };
        var jsonPath = Path.Combine(directory, "20260601_100000.json");

        await logger.WriteJsonSummaryAsync(jsonPath, summary, CancellationToken.None);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(jsonPath));
        Assert.Equal("Windows", document.RootElement.GetProperty("summary").GetProperty("windowsVersion").GetString());
    }
}
