using ResetFlow.Core.Models;

namespace ResetFlow.Core.Features;

public abstract class FeatureBase : IFeature
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Category { get; }
    public abstract string Description { get; }
    public abstract AutomationLevel AutomationLevel { get; }
    public virtual bool RequiresAdmin => false;
    public virtual bool RequiresReboot => false;
    public virtual bool ExcludeOnLaptop => false;
    public abstract RollbackCapability RollbackCapability { get; }
    public virtual int Order => 100;

    public virtual Task<FeatureResult> PrecheckAsync(FeatureContext context) =>
        Task.FromResult(FeatureResult.Success(Id, FeatureStep.Precheck));

    public virtual Task<FeatureResult> BackupAsync(FeatureContext context) =>
        Task.FromResult(FeatureResult.Success(Id, FeatureStep.Backup, "No backup required"));

    public abstract Task<FeatureResult> ApplyAsync(FeatureContext context);

    public virtual Task<FeatureResult> VerifyAsync(FeatureContext context) =>
        Task.FromResult(FeatureResult.Success(Id, FeatureStep.Verify));

    public virtual Task<RollbackResult> RollbackAsync(FeatureContext context) =>
        Task.FromResult(new RollbackResult(Id, FeatureStatus.Skipped, "Rollback is not implemented."));
}
