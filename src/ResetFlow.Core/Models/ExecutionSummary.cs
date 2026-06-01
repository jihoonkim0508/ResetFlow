namespace ResetFlow.Core.Models;

public sealed class ExecutionSummary
{
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset FinishedAt { get; set; }
    public string WindowsVersion { get; init; } = "";
    public bool IsAdministrator { get; init; }
    public DeviceType DeviceType { get; init; }
    public string GpuInfo { get; init; } = "";
    public List<string> SelectedFeatureIds { get; } = [];
    public List<FeatureRunSummary> Features { get; } = [];
    public List<RollbackResult> Rollbacks { get; } = [];
    public List<string> ManualChecklist { get; } = [];
    public string TextLogPath { get; set; } = "";
    public string JsonLogPath { get; set; } = "";
    public bool RequiresReboot => Features.Any(feature => feature.Status == FeatureStatus.Success && feature.RequiresReboot);
    public bool Succeeded => Features.All(feature => feature.Status is FeatureStatus.Success or FeatureStatus.Skipped or FeatureStatus.ManualRequired);
}

public sealed record FeatureRunSummary(
    string FeatureId,
    string Name,
    string Category,
    FeatureStatus Status,
    string Message,
    bool RequiresReboot);

public sealed record RollbackResult(
    string FeatureId,
    FeatureStatus Status,
    string Message);
