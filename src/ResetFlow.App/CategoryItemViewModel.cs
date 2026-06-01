namespace ResetFlow.App;

public sealed class CategoryItemViewModel(string name) : ObservableObject
{
    public string Name { get; } = name;
}
