namespace ResetFlow.Core.Models;

public sealed record FeatureResult(
    string FeatureId,
    FeatureStep Step,
    FeatureStatus Status,
    string Message,
    Exception? Exception = null,
    IReadOnlyList<string>? Notes = null)
{
    public static FeatureResult Success(string featureId, FeatureStep step, string message = "OK", IReadOnlyList<string>? notes = null) =>
        new(featureId, step, FeatureStatus.Success, message, null, notes);

    public static FeatureResult Skipped(string featureId, FeatureStep step, string message, IReadOnlyList<string>? notes = null) =>
        new(featureId, step, FeatureStatus.Skipped, message, null, notes);

    public static FeatureResult Manual(string featureId, string message, IReadOnlyList<string> notes) =>
        new(featureId, FeatureStep.Apply, FeatureStatus.ManualRequired, message, null, notes);

    public static FeatureResult Failed(string featureId, FeatureStep step, string message, Exception? exception = null) =>
        new(featureId, step, FeatureStatus.Failed, message, exception);
}
