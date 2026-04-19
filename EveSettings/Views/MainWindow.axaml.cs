using Avalonia.Controls;
using EveSettings.ViewModels;

namespace EveSettings.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        Opened += async (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AttachShell(this);
                await vm.Sync.InitializeAsync();
            }
        };
    }
}
