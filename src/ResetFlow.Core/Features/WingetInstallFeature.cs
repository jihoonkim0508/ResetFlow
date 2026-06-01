using ResetFlow.Core.Models;

namespace ResetFlow.Core.Features;

public sealed class WingetInstallFeature(
    string id,
    string name,
    string category,
    string description,
    Func<FeatureContext, string> packageId,
    int order) : FeatureBase
{
    public override string Id => id;
    public override string Name => name;
    public override string Category => category;
    public override string Description => description;
    public override AutomationLevel AutomationLevel => AutomationLevel.Automatic;
    public override bool RequiresAdmin => false;
    public override RollbackCapability RollbackCapability => RollbackCapability.Partial;
    public override int Order => order;

    public override async Task<FeatureResult> PrecheckAsync(FeatureContext context)
    {
        var result = await RunWingetAsync(context, $"list --id {Quote(packageId(context))} --exact --accept-source-agreements");
        context.Logger.Command(result);
        if (IsAlreadyInstalled(result))
        {
            return FeatureResult.Skipped(Id, FeatureStep.Precheck, $"{packageId(context)} is already installed.");
        }

        return FeatureResult.Success(Id, FeatureStep.Precheck, $"{packageId(context)} is not installed.");
    }

    public override async Task<FeatureResult> ApplyAsync(FeatureContext context)
    {
        var id = packageId(context);
        var result = await RunWingetAsync(context, $"install --id {Quote(id)} --exact --silent --accept-package-agreements --accept-source-agreements");
        context.Logger.Command(result);
        return result.Succeeded
            ? FeatureResult.Success(Id, FeatureStep.Apply, $"{id} installed.")
            : FeatureResult.Failed(Id, FeatureStep.Apply, $"winget install failed for {id}: {result.StandardError}");
    }

    public override Task<FeatureResult> VerifyAsync(FeatureContext context) =>
        Task.FromResult(FeatureResult.Success(Id, FeatureStep.Verify, "winget command completed."));

    public override Task<RollbackResult> RollbackAsync(FeatureContext context) =>
        Task.FromResult(new RollbackResult(Id, FeatureStatus.Skipped, "Program installation rollback is partial; automatic uninstall is intentionally not performed."));

    public static bool IsAlreadyInstalled(CommandResultLike result) =>
        result.ExitCode == 0 && !ContainsNoInstalledPackageMessage(result.StandardOutput + result.StandardError);

    public static string BuildListArguments(string packageId) =>
        $"list --id {Quote(packageId)} --exact --accept-source-agreements";

    public static string BuildInstallArguments(string packageId) =>
        $"install --id {Quote(packageId)} --exact --silent --accept-package-agreements --accept-source-agreements";

    private static async Task<SystemServices.CommandResult> RunWingetAsync(FeatureContext context, string arguments) =>
        await context.CommandRunner.RunAsync("winget", arguments, context.CancellationToken);

    private static bool IsAlreadyInstalled(SystemServices.CommandResult result) =>
        IsAlreadyInstalled(new CommandResultLike(result.ExitCode, result.StandardOutput, result.StandardError));

    private static bool ContainsNoInstalledPackageMessage(string text) =>
        text.Contains("No installed package found", StringComparison.OrdinalIgnoreCase)
        || text.Contains("No package found", StringComparison.OrdinalIgnoreCase);

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
}

public sealed record CommandResultLike(int ExitCode, string StandardOutput, string StandardError);
