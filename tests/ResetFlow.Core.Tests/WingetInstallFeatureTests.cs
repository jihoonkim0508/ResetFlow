using ResetFlow.Core.Features;

namespace ResetFlow.Core.Tests;

public sealed class WingetInstallFeatureTests
{
    [Fact]
    public void BuildArguments_UseExactIdAndAgreementFlags()
    {
        Assert.Equal("list --id Google.Chrome --exact --accept-source-agreements", WingetInstallFeature.BuildListArguments("Google.Chrome"));
        Assert.Equal("install --id Microsoft.VisualStudioCode --exact --silent --accept-package-agreements --accept-source-agreements", WingetInstallFeature.BuildInstallArguments("Microsoft.VisualStudioCode"));
    }

    [Fact]
    public void IsAlreadyInstalled_RecognizesListResults()
    {
        Assert.True(WingetInstallFeature.IsAlreadyInstalled(new CommandResultLike(0, "Google Chrome Google.Chrome 1.0", "")));
        Assert.False(WingetInstallFeature.IsAlreadyInstalled(new CommandResultLike(1, "", "No installed package found matching input criteria.")));
    }
}
