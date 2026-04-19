using CommunityToolkit.Mvvm.ComponentModel;
using EveSettings.Core.Models;

namespace EveSettings.ViewModels;

public partial class CharacterNamedFileItem : ViewModelBase
{
    public CharacterNamedFileItem(SettingsFileEntry entry) => Entry = entry;

    public SettingsFileEntry Entry { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayLine))]
    private string? _resolvedName;

    public string DisplayLine => FileDisplayFormatter.Format(Entry, ResolvedName);
}
