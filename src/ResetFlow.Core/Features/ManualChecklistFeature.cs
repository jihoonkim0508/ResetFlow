using ResetFlow.Core.Models;

namespace ResetFlow.Core.Features;

public sealed class ManualChecklistFeature(
    string id,
    string name,
    string category,
    string description,
    IReadOnlyList<string> checklist,
    int order) : FeatureBase
{
    public override string Id => id;
    public override string Name => name;
    public override string Category => category;
    public override string Description => description;
    public override AutomationLevel AutomationLevel => AutomationLevel.ManualChecklist;
    public override RollbackCapability RollbackCapability => RollbackCapability.None;
    public override int Order => order;

    public override Task<FeatureResult> ApplyAsync(FeatureContext context) =>
        Task.FromResult(FeatureResult.Manual(Id, "Manual confirmation is required.", checklist));
}
