using ResetFlow.Core.Features;

namespace ResetFlow.Core.Tests;

public sealed class FeatureCatalogTests
{
    [Fact]
    public void SelectEnabled_FiltersDisabledFeatures_AndSortsByOrder()
    {
        var catalog = FeatureCatalog.CreateDefault();
        var selected = catalog.SelectEnabled(new Dictionary<string, bool>
        {
            ["apps.install.python"] = true,
            ["windows.mouse.disableAcceleration"] = true,
            ["apps.install.git"] = false
        });

        Assert.Equal(["windows.mouse.disableAcceleration", "apps.install.python"], selected.Select(feature => feature.Id));
    }
}
