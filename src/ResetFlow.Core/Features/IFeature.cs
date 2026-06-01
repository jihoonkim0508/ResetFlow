using ResetFlow.Core.Models;

namespace ResetFlow.Core.Features;

public interface IFeature
{
    string Id { get; }
    string Name { get; }
    string Category { get; }
    string Description { get; }
    AutomationLevel AutomationLevel { get; }
    bool RequiresAdmin { get; }
    bool RequiresReboot { get; }
    bool ExcludeOnLaptop { get; }
    RollbackCapability RollbackCapability { get; }
    int Order { get; }

    Task<FeatureResult> PrecheckAsync(FeatureContext context);
    Task<FeatureResult> BackupAsync(FeatureContext context);
    Task<FeatureResult> ApplyAsync(FeatureContext context);
    Task<FeatureResult> VerifyAsync(FeatureContext context);
    Task<RollbackResult> RollbackAsync(FeatureContext context);
}
