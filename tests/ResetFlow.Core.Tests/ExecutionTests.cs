using ResetFlow.Core.Execution;
using ResetFlow.Core.Models;

namespace ResetFlow.Core.Tests;

public sealed class ExecutionTests
{
    [Fact]
    public async Task ExecuteAsync_RollsBackSuccessfulFeaturesInReverseOrder_WhenLaterFeatureFails()
    {
        var events = new List<string>();
        var features = new[]
        {
            new TestFeature("one", 1, events),
            new TestFeature("two", 2, events),
            new TestFeature("three", 3, events, failApply: true)
        };

        var summary = await new FeatureExecutor().ExecuteAsync(features, TestContextFactory.Create());

        Assert.Contains(summary.Features, feature => feature.FeatureId == "three" && feature.Status == FeatureStatus.Failed);
        Assert.Equal(["apply:one", "apply:two", "apply:three", "rollback:two", "rollback:one"], events);
        Assert.Equal(["two", "one"], summary.Rollbacks.Select(rollback => rollback.FeatureId));
    }
}
