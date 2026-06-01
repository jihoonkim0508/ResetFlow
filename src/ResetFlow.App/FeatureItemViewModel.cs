using ResetFlow.Core.Features;

namespace ResetFlow.App;

public sealed class FeatureItemViewModel : ObservableObject
{
    private bool _isSelected;
    private bool _isEnabled = true;
    private string _status = "Pending";

    public FeatureItemViewModel(IFeature feature, bool isSelected)
    {
        Feature = feature;
        _isSelected = isSelected;
    }

    public IFeature Feature { get; }
    public string Id => Feature.Id;
    public string Name => Feature.Name;
    public string Category => Feature.Category;
    public string Description => Feature.Description;
    public string AutomationLevel => Feature.AutomationLevel.ToString();
    public string RequiresAdmin => Feature.RequiresAdmin ? "Yes" : "No";
    public string RequiresReboot => Feature.RequiresReboot ? "Yes" : "No";
    public string RollbackCapability => Feature.RollbackCapability.ToString();
    public string ExcludeOnLaptop => Feature.ExcludeOnLaptop ? "Yes" : "No";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}
