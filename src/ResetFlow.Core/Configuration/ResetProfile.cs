using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResetFlow.Core.Configuration;

public sealed class ResetProfile
{
    public string ProfileName { get; set; } = "default";
    public Dictionary<string, bool> Features { get; set; } = [];
    public ChromeSettings Chrome { get; set; } = new();
    public FolderSettings Folders { get; set; } = new();
    public ConditionSettings Conditions { get; set; } = new();
    public AppSettings Apps { get; set; } = new();

    [JsonIgnore]
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static ResetProfile CreateDefault()
    {
        var profile = new ResetProfile();
        foreach (var id in DefaultFeatureIds)
        {
            profile.Features[id] = false;
        }

        profile.Folders.Documents.AddRange([
            "toeic",
            "카카오톡 받은 파일",
            "Tools/구라 제거기"
        ]);
        profile.Folders.Pictures.AddRange([
            "스크린샷",
            "저장된 사진/직인",
            "저장된 사진/푸들 프사",
            "저장된 사진/배경사진"
        ]);

        return profile;
    }

    public static IReadOnlyList<string> DefaultFeatureIds { get; } =
    [
        "windows.mouse.disableAcceleration",
        "windows.explorer.openToThisPC",
        "windows.explorer.disablePrivacy",
        "windows.explorer.showFileExtensions",
        "windows.explorer.showHiddenFiles",
        "windows.theme.darkMode",
        "folders.documents",
        "folders.pictures",
        "apps.install.chrome",
        "apps.install.git",
        "apps.install.vscode",
        "apps.install.python",
        "manual.chromeLogin",
        "manual.driverCheck",
        "manual.nvidiaSettings",
        "manual.potPlayerNormalizer",
        "manual.startMenuCleanup",
        "manual.taskbarCleanup"
    ];
}

public sealed class ChromeSettings
{
    public List<string> Accounts { get; set; } = [];
}

public sealed class FolderSettings
{
    public List<string> Documents { get; set; } = [];
    public List<string> Pictures { get; set; } = [];
    public string TodoFile { get; set; } = "TO DO.txt";
}

public sealed class ConditionSettings
{
    public bool SkipLaptopOnlyFeatures { get; set; } = true;
}

public sealed class AppSettings
{
    public string ChromePackageId { get; set; } = "Google.Chrome";
    public string GitPackageId { get; set; } = "Git.Git";
    public string VsCodePackageId { get; set; } = "Microsoft.VisualStudioCode";
    public string PythonPackageId { get; set; } = "Python.Python.3.12";
}
