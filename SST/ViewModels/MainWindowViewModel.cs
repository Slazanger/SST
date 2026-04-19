using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SST.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Sync = new SyncViewModel();
    }

    public SyncViewModel Sync { get; }

    public void AttachShell(Window shell)
    {
        Sync.AttachShell(shell);
    }
}
