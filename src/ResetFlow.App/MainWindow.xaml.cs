using System.Windows;

namespace ResetFlow.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.InitializeAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "ResetFlow startup error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
