using ResetFlow.Core.Features;
using ResetFlow.Core.Models;

namespace ResetFlow.Core.Execution;

public sealed class FeatureExecutor
{
    public async Task<ExecutionSummary> ExecuteAsync(
        IReadOnlyList<IFeature> selectedFeatures,
        FeatureContext context,
        IProgress<FeatureProgress>? progress = null)
    {
        var summary = new ExecutionSummary
        {
            StartedAt = context.Clock.Now,
            WindowsVersion = context.System.WindowsVersion,
            IsAdministrator = context.System.IsAdministrator,
            DeviceType = context.System.DeviceType,
            GpuInfo = context.System.GpuInfo,
            TextLogPath = context.Logger.TextLogPath
        };
        summary.SelectedFeatureIds.AddRange(selectedFeatures.Select(feature => feature.Id));

        if (selectedFeatures.Any(feature => feature.RequiresAdmin) && !context.System.IsAdministrator)
        {
            summary.Features.Add(new FeatureRunSummary("system.admin", "Administrator check", "System", FeatureStatus.Failed, "Selected features require administrator privileges.", false));
            summary.FinishedAt = context.Clock.Now;
            return summary;
        }

        var applied = new List<IFeature>();
        for (var index = 0; index < selectedFeatures.Count; index++)
        {
            var feature = selectedFeatures[index];
            progress?.Report(new FeatureProgress(index, selectedFeatures.Count, feature.Name, summary));

            if (context.Profile.Conditions.SkipLaptopOnlyFeatures && feature.ExcludeOnLaptop && context.System.DeviceType == DeviceType.Laptop)
            {
                summary.Features.Add(new FeatureRunSummary(feature.Id, feature.Name, feature.Category, FeatureStatus.Skipped, "Skipped on laptop.", feature.RequiresReboot));
                continue;
            }

            try
            {
                var precheck = await feature.PrecheckAsync(context);
                if (precheck.Status == FeatureStatus.Skipped)
                {
                    summary.Features.Add(new FeatureRunSummary(feature.Id, feature.Name, feature.Category, FeatureStatus.Skipped, precheck.Message, feature.RequiresReboot));
                    continue;
                }

                EnsureSuccess(precheck);
                EnsureSuccess(await feature.BackupAsync(context));
                var apply = await feature.ApplyAsync(context);
                if (apply.Status == FeatureStatus.ManualRequired)
                {
                    summary.ManualChecklist.AddRange(apply.Notes ?? []);
                    summary.Features.Add(new FeatureRunSummary(feature.Id, feature.Name, feature.Category, FeatureStatus.ManualRequired, apply.Message, feature.RequiresReboot));
                    continue;
                }

                EnsureSuccess(apply);
                EnsureSuccess(await feature.VerifyAsync(context));
                applied.Add(feature);
                summary.Features.Add(new FeatureRunSummary(feature.Id, feature.Name, feature.Category, FeatureStatus.Success, "Completed.", feature.RequiresReboot));
            }
            catch (Exception exception)
            {
                context.Logger.Error($"{feature.Id} failed.", exception);
                summary.Features.Add(new FeatureRunSummary(feature.Id, feature.Name, feature.Category, FeatureStatus.Failed, exception.Message, feature.RequiresReboot));
                foreach (var completedFeature in applied.Where(candidate => candidate.RollbackCapability != RollbackCapability.None).Reverse<IFeature>())
                {
                    try
                    {
                        summary.Rollbacks.Add(await completedFeature.RollbackAsync(context));
                    }
                    catch (Exception rollbackException)
                    {
                        summary.Rollbacks.Add(new RollbackResult(completedFeature.Id, FeatureStatus.Failed, rollbackException.Message));
                    }
                }

                break;
            }
        }

        summary.FinishedAt = context.Clock.Now;
        return summary;
    }

    private static void EnsureSuccess(FeatureResult result)
    {
        if (result.Status != FeatureStatus.Success)
        {
            throw result.Exception ?? new InvalidOperationException(result.Message);
        }
    }
}

public sealed record FeatureProgress(int CurrentIndex, int Total, string CurrentFeatureName, ExecutionSummary Summary);
