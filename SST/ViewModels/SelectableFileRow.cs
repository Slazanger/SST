using CommunityToolkit.Mvvm.ComponentModel;
using SST.Core.Models;

namespace SST.ViewModels;

public partial class SelectableFileRow : ViewModelBase
{
    public SelectableFileRow(SettingsFileEntry entry) => Entry = entry;

    public SettingsFileEntry Entry { get; }

    [ObservableProperty] private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLine))]
    private string? _resolvedName;

    public string DisplayLine => FileDisplayFormatter.Format(Entry, ResolvedName);
}
