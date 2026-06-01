namespace ResetFlow.Core.Features;

public sealed class FeatureCatalog
{
    private readonly IReadOnlyList<IFeature> _features;

    public FeatureCatalog(IReadOnlyList<IFeature> features)
    {
        _features = features;
    }

    public IReadOnlyList<IFeature> Features => _features;

    public IEnumerable<string> Categories => _features
        .Select(feature => feature.Category)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(category => category);

    public IReadOnlyList<IFeature> SelectEnabled(IReadOnlyDictionary<string, bool> selections) =>
        _features
            .Where(feature => selections.TryGetValue(feature.Id, out var enabled) && enabled)
            .OrderBy(feature => feature.Order)
            .ThenBy(feature => feature.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static FeatureCatalog CreateDefault() =>
        new([
            new RegistryValueFeature(
                "windows.mouse.disableAcceleration",
                "Disable mouse acceleration",
                "Windows settings",
                "Backs up HKCU mouse settings and disables pointer acceleration.",
                @"Control Panel\Mouse",
                new Dictionary<string, object>
                {
                    ["MouseSpeed"] = "0",
                    ["MouseThreshold1"] = "0",
                    ["MouseThreshold2"] = "0"
                },
                10),
            new RegistryValueFeature(
                "windows.explorer.openToThisPC",
                "Open Explorer to This PC",
                "Windows settings",
                "Sets File Explorer launch target to This PC.",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                new Dictionary<string, object> { ["LaunchTo"] = 1 },
                20),
            new RegistryValueFeature(
                "windows.explorer.disablePrivacy",
                "Disable Explorer privacy recents",
                "Windows settings",
                "Disables recent and frequent items in Explorer.",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer",
                new Dictionary<string, object>
                {
                    ["ShowRecent"] = 0,
                    ["ShowFrequent"] = 0
                },
                21),
            new RegistryValueFeature(
                "windows.explorer.showFileExtensions",
                "Show file extensions",
                "Windows settings",
                "Shows known file extensions in Explorer.",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                new Dictionary<string, object> { ["HideFileExt"] = 0 },
                22),
            new RegistryValueFeature(
                "windows.explorer.showHiddenFiles",
                "Show hidden files",
                "Windows settings",
                "Shows hidden files in Explorer.",
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
                new Dictionary<string, object> { ["Hidden"] = 1 },
                23),
            new RegistryValueFeature(
                "windows.theme.darkMode",
                "Dark mode",
                "Windows settings",
                "Enables dark app and system theme values.",
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                new Dictionary<string, object>
                {
                    ["AppsUseLightTheme"] = 0,
                    ["SystemUsesLightTheme"] = 0
                },
                30),
            new FolderFeature(
                "folders.documents",
                "Create Documents folders",
                "Folders",
                "Creates the personal Documents folder structure without overwriting existing files.",
                Environment.SpecialFolder.MyDocuments,
                context => context.Profile.Folders.Documents,
                true,
                40),
            new FolderFeature(
                "folders.pictures",
                "Create Pictures folders",
                "Folders",
                "Creates the personal Pictures folder structure without overwriting existing files.",
                Environment.SpecialFolder.MyPictures,
                context => context.Profile.Folders.Pictures,
                false,
                41),
            new WingetInstallFeature(
                "apps.install.chrome",
                "Install Chrome",
                "App install",
                "Installs Google Chrome with winget if it is not already installed.",
                context => context.Profile.Apps.ChromePackageId,
                60),
            new WingetInstallFeature(
                "apps.install.git",
                "Install Git",
                "App install",
                "Installs Git with winget if it is not already installed.",
                context => context.Profile.Apps.GitPackageId,
                61),
            new WingetInstallFeature(
                "apps.install.vscode",
                "Install VS Code",
                "App install",
                "Installs Visual Studio Code with winget if it is not already installed.",
                context => context.Profile.Apps.VsCodePackageId,
                62),
            new WingetInstallFeature(
                "apps.install.python",
                "Install Python",
                "App install",
                "Installs Python with winget if it is not already installed.",
                context => context.Profile.Apps.PythonPackageId,
                63),
            new ManualChecklistFeature(
                "manual.chromeLogin",
                "Chrome account login",
                "Manual checklist",
                "Shows Chrome account login checks.",
                ["Open Chrome", "Sign in to the required accounts from the profile JSON", "Confirm sync state"],
                100),
            new ManualChecklistFeature(
                "manual.driverCheck",
                "3DP and driver check",
                "Manual checklist",
                "Driver installation and validation remain manual in the MVP.",
                ["Run the preferred driver check tool", "Install only confirmed missing drivers", "Reboot if prompted"],
                101),
            new ManualChecklistFeature(
                "manual.nvidiaSettings",
                "NVIDIA performance settings",
                "Manual checklist",
                "NVIDIA Control Panel settings remain manual in the MVP.",
                ["Open NVIDIA Control Panel", "Set power management to prefer maximum performance", "Confirm low latency preference"],
                102),
            new ManualChecklistFeature(
                "manual.potPlayerNormalizer",
                "PotPlayer normalizer",
                "Manual checklist",
                "PotPlayer internal settings remain manual in the MVP.",
                ["Open PotPlayer preferences", "Enable normalizer setting", "Confirm playback volume behavior"],
                103),
            new ManualChecklistFeature(
                "manual.startMenuCleanup",
                "Start menu cleanup",
                "Manual checklist",
                "Start pinning is tracked as a manual checklist.",
                ["Remove unwanted pins", "Pin preferred development and utility apps"],
                104),
            new ManualChecklistFeature(
                "manual.taskbarCleanup",
                "Taskbar cleanup",
                "Manual checklist",
                "Taskbar pinning is tracked as a manual checklist.",
                ["Unpin unwanted defaults", "Pin Chrome, Explorer, terminal, and development tools"],
                105)
        ]);
}
