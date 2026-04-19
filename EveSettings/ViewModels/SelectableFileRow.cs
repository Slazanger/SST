using CommunityToolkit.Mvvm.ComponentModel;
using EveSettings.Core.Models;

namespace EveSettings.ViewModels;

public partial class SelectableFileRow : ViewModelBase
{
    public SelectableFileRow(SettingsFileEntry entry) => Entry = entry;

    public SettingsFileEntry Entry { get; }

    [ObservableProperty] private bool _isSelected;
}
