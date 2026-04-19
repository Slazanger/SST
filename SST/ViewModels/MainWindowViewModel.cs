using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SST.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
    {
        Sync = new SyncViewModel();
        Inspector = new InspectorViewModel();
    }

    public SyncViewModel Sync { get; }
    public InspectorViewModel Inspector { get; }

    public void AttachShell(Window shell)
    {
        Sync.AttachShell(shell);
        Inspector.AttachShell(shell);
    }
}
