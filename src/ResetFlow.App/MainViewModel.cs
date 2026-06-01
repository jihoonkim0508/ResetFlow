using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using ResetFlow.Core.Configuration;
using ResetFlow.Core.Execution;
using ResetFlow.Core.Features;
using ResetFlow.Core.IO;
using ResetFlow.Core.Logging;
using ResetFlow.Core.Models;
using ResetFlow.Core.Registry;
using ResetFlow.Core.SystemServices;

namespace ResetFlow.App;

public sealed class MainViewModel : ObservableObject
{
    private readonly IFileSystem _fileSystem = new RealFileSystem();
    private readonly ICommandRunner _commandRunner = new ProcessCommandRunner();
    private readonly IClock _clock = new SystemClock();
    private readonly FeatureCatalog _catalog = FeatureCatalog.CreateDefault();
    private readonly FeatureExecutor _executor = new();
    private AppStorage? _storage;
    private ResetProfile _profile = ResetProfile.CreateDefault();
    private SystemSnapshot _system = new("Unknown", false, ResetFlow.Core.Models.DeviceType.Unknown, "Unknown");
    private FeatureItemViewModel? _selectedFeature;
    private CategoryItemViewModel? _selectedCategory;
    private bool _isRunning;
    private double _progressValue;
    private string _currentFeature = "";
    private string _summaryText = "Load the profile, select features, then start.";
    private string _lastLogPath = "";
    private string _lastJsonPath = "";

    public MainViewModel()
    {
        Features = new ObservableCollection<FeatureItemViewModel>();
        Categories = new ObservableCollection<CategoryItemViewModel>();
        LogLines = new ObservableCollection<string>();

        SelectAllCommand = new RelayCommand(_ => SetAllAsync(true), _ => !IsRunning);
        SelectNoneCommand = new RelayCommand(_ => SetAllAsync(false), _ => !IsRunning);
        SelectCategoryCommand = new RelayCommand(_ => SetCategoryAsync(true), _ => !IsRunning && SelectedCategory is not null);
        ClearCategoryCommand = new RelayCommand(_ => SetCategoryAsync(false), _ => !IsRunning && SelectedCategory is not null);
        StartCommand = new RelayCommand(_ => StartAsync(), _ => !IsRunning);
        OpenLogCommand = new RelayCommand(_ => OpenPathAsync(_lastLogPath), _ => !string.IsNullOrWhiteSpace(_lastLogPath));
        OpenProfileCommand = new RelayCommand(_ => OpenPathAsync(_storage?.ProfilePath), _ => _storage is not null);
        RestartElevatedCommand = new RelayCommand(_ => RestartElevatedAsync(), _ => !IsRunning);
    }

    public ObservableCollection<FeatureItemViewModel> Features { get; }
    public ObservableCollection<CategoryItemViewModel> Categories { get; }
    public ObservableCollection<string> LogLines { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand SelectNoneCommand { get; }
    public RelayCommand SelectCategoryCommand { get; }
    public RelayCommand ClearCategoryCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand OpenLogCommand { get; }
    public RelayCommand OpenProfileCommand { get; }
    public RelayCommand RestartElevatedCommand { get; }

    public string WindowsVersion => _system.WindowsVersion;
    public string AdminStatus => _system.IsAdministrator ? "Administrator" : "Standard user";
    public string DeviceType => _system.DeviceType.ToString();
    public string GpuInfo => _system.GpuInfo;
    public int SuccessCount => Features.Count(feature => feature.Status == "Success");
    public int FailedCount => Features.Count(feature => feature.Status == "Failed");
    public int SkippedCount => Features.Count(feature => feature.Status is "Skipped" or "ManualRequired");
    public bool RequiresElevation => Features.Any(feature => feature.IsSelected && feature.Feature.RequiresAdmin) && !_system.IsAdministrator;

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                foreach (var feature in Features)
                {
                    feature.IsEnabled = !value;
                }

                RaiseCommandStates();
            }
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string CurrentFeature
    {
        get => _currentFeature;
        private set => SetProperty(ref _currentFeature, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public FeatureItemViewModel? SelectedFeature
    {
        get => _selectedFeature;
        set => SetProperty(ref _selectedFeature, value);
    }

    public CategoryItemViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public async Task InitializeAsync()
    {
        _storage = AppStorage.Resolve(_fileSystem, AppContext.BaseDirectory);
        _profile = await new ProfileLoader(_fileSystem).LoadOrCreateAsync(_storage.ProfilePath, CancellationToken.None);
        foreach (var id in ResetProfile.DefaultFeatureIds)
        {
            _profile.Features.TryAdd(id, false);
        }

        _system = await new WindowsSystemInfoProvider(_commandRunner).CaptureAsync(CancellationToken.None);
        Features.Clear();
        foreach (var feature in _catalog.Features)
        {
            var item = new FeatureItemViewModel(feature, _profile.Features.TryGetValue(feature.Id, out var enabled) && enabled);
            item.PropertyChanged += async (_, args) =>
            {
                if (args.PropertyName == nameof(FeatureItemViewModel.IsSelected))
                {
                    _profile.Features[item.Id] = item.IsSelected;
                    OnPropertyChanged(nameof(RequiresElevation));
                    await SaveProfileAsync();
                }
            };
            Features.Add(item);
        }

        Categories.Clear();
        foreach (var category in _catalog.Categories)
        {
            Categories.Add(new CategoryItemViewModel(category));
        }

        OnPropertyChanged(nameof(WindowsVersion));
        OnPropertyChanged(nameof(AdminStatus));
        OnPropertyChanged(nameof(DeviceType));
        OnPropertyChanged(nameof(GpuInfo));
        OnPropertyChanged(nameof(RequiresElevation));
        SummaryText = $"Profile: {_profile.ProfileName}. Storage: {_storage.RootPath}";
        RaiseCommandStates();
    }

    private async Task StartAsync()
    {
        if (_storage is null)
        {
            return;
        }

        if (RequiresElevation)
        {
            SummaryText = "Selected features require administrator privileges. Restart elevated and run again.";
            return;
        }

        IsRunning = true;
        ProgressValue = 0;
        CurrentFeature = "";
        LogLines.Clear();
        foreach (var feature in Features)
        {
            feature.Status = "Pending";
        }

        var stamp = _clock.Now.ToString("yyyyMMdd_HHmmss");
        var logger = new RunLogger(Path.Combine(_storage.LogsPath, $"{stamp}.log"));
        var context = new FeatureContext
        {
            Profile = _profile,
            Storage = _storage,
            Logger = logger,
            Registry = new CurrentUserRegistryStore(),
            FileSystem = _fileSystem,
            CommandRunner = _commandRunner,
            System = _system,
            Clock = _clock,
            CancellationToken = CancellationToken.None,
            RunBackupPath = Path.Combine(_storage.BackupsPath, stamp)
        };

        try
        {
            var selectedIds = Features.Where(feature => feature.IsSelected).Select(feature => feature.Id).ToDictionary(id => id, _ => true);
            var selectedFeatures = _catalog.SelectEnabled(selectedIds);
            if (selectedFeatures.Count == 0)
            {
                SummaryText = "No features selected.";
                return;
            }

            var progress = new Progress<FeatureProgress>(UpdateProgress);
            var summary = await _executor.ExecuteAsync(selectedFeatures, context, progress);
            foreach (var featureSummary in summary.Features)
            {
                var item = Features.FirstOrDefault(feature => feature.Id == featureSummary.FeatureId);
                if (item is not null)
                {
                    item.Status = featureSummary.Status.ToString();
                }
            }

            _lastLogPath = logger.TextLogPath;
            _lastJsonPath = Path.Combine(_storage.LogsPath, $"{stamp}.json");
            summary.JsonLogPath = _lastJsonPath;
            summary.TextLogPath = _lastLogPath;
            await logger.FlushTextAsync(CancellationToken.None);
            await logger.WriteJsonSummaryAsync(_lastJsonPath, summary, CancellationToken.None);
            LogLines.Clear();
            foreach (var line in logger.Lines.TakeLast(120))
            {
                LogLines.Add(line);
            }

            SummaryText = BuildSummary(summary);
            ProgressValue = 100;
            OnPropertyChanged(nameof(SuccessCount));
            OnPropertyChanged(nameof(FailedCount));
            OnPropertyChanged(nameof(SkippedCount));
        }
        finally
        {
            IsRunning = false;
            RaiseCommandStates();
        }
    }

    private void UpdateProgress(FeatureProgress progress)
    {
        CurrentFeature = progress.CurrentFeatureName;
        ProgressValue = progress.Total == 0 ? 0 : (double)progress.CurrentIndex / progress.Total * 100d;
    }

    private string BuildSummary(ExecutionSummary summary)
    {
        var status = summary.Succeeded ? "Completed" : "Stopped after failure";
        var reboot = summary.RequiresReboot ? "Reboot required." : "No reboot required.";
        return $"{status}. Success: {summary.Features.Count(feature => feature.Status == FeatureStatus.Success)}, Failed: {summary.Features.Count(feature => feature.Status == FeatureStatus.Failed)}, Skipped/manual: {summary.Features.Count(feature => feature.Status is FeatureStatus.Skipped or FeatureStatus.ManualRequired)}. {reboot} Logs: {summary.TextLogPath}";
    }

    private async Task SetAllAsync(bool selected)
    {
        foreach (var feature in Features)
        {
            feature.IsSelected = selected;
        }

        await SaveProfileAsync();
    }

    private async Task SetCategoryAsync(bool selected)
    {
        if (SelectedCategory is null)
        {
            return;
        }

        foreach (var feature in Features.Where(feature => feature.Category == SelectedCategory.Name))
        {
            feature.IsSelected = selected;
        }

        await SaveProfileAsync();
    }

    private async Task SaveProfileAsync()
    {
        if (_storage is null)
        {
            return;
        }

        await _fileSystem.WriteAllTextAsync(_storage.ProfilePath, JsonSerializer.Serialize(_profile, ResetProfile.JsonOptions), CancellationToken.None);
    }

    private static Task OpenPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    private static Task RestartElevatedAsync()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return Task.CompletedTask;
        }

        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true, Verb = "runas" });
        Application.Current.Shutdown();
        return Task.CompletedTask;
    }

    private void RaiseCommandStates()
    {
        SelectAllCommand.RaiseCanExecuteChanged();
        SelectNoneCommand.RaiseCanExecuteChanged();
        SelectCategoryCommand.RaiseCanExecuteChanged();
        ClearCategoryCommand.RaiseCanExecuteChanged();
        StartCommand.RaiseCanExecuteChanged();
        OpenLogCommand.RaiseCanExecuteChanged();
        OpenProfileCommand.RaiseCanExecuteChanged();
        RestartElevatedCommand.RaiseCanExecuteChanged();
    }
}
