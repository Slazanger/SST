using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EveSettings.Core;
using EveSettings.Core.Models;
using EveSettings.Services;

namespace EveSettings.ViewModels;

public partial class SyncViewModel : ViewModelBase
{
    private readonly AppSettingsStore _settingsStore = new();
    private readonly SettingsCopyService _copyService = new();
    private Window? _shell;

    [ObservableProperty] private string? _eveRootPath;
    [ObservableProperty] private string _statusMessage = "Select your EVE local settings folder, then scan.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private SettingsFileEntry? _masterChar;
    [ObservableProperty] private SettingsFileEntry? _masterUser;

    public ObservableCollection<SettingsFileEntry> CharFiles { get; } = [];
    public ObservableCollection<SettingsFileEntry> UserFiles { get; } = [];
    public ObservableCollection<SelectableFileRow> TargetRows { get; } = [];

    public void AttachShell(Window shell) => _shell = shell;

    public async Task InitializeAsync()
    {
        var s = await _settingsStore.LoadAsync();
        EveRootPath = s.LastEveRootPath;
        if (string.IsNullOrWhiteSpace(EveRootPath))
            EveRootPath = EvePaths.GetDefaultWindowsCcpEveRoot();

        if (Directory.Exists(EveRootPath))
            await ScanAsync();
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (_shell is null)
        {
            StatusMessage = "Window is not ready yet.";
            return;
        }

        var suggested = EveRootPath;
        if (string.IsNullOrWhiteSpace(suggested) || !Directory.Exists(suggested))
            suggested = EvePaths.GetDefaultWindowsCcpEveRoot();

        IStorageFolder? start = null;
        try
        {
            start = await _shell.StorageProvider.TryGetFolderFromPathAsync(suggested);
        }
        catch
        {
            // ignore invalid suggested path
        }

        var folders = await _shell.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select EVE local settings folder (CCP/EVE or a server folder)",
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });

        var picked = folders.FirstOrDefault();
        if (picked is null)
            return;

        EveRootPath = picked.Path.LocalPath;
        await _settingsStore.SaveAsync(new AppSettings { LastEveRootPath = EveRootPath });
        await ScanAsync();
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(EveRootPath))
        {
            StatusMessage = "Choose a folder first.";
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Yield();
            var result = SettingsScanner.Scan(EveRootPath);

            CharFiles.Clear();
            UserFiles.Clear();
            TargetRows.Clear();
            MasterChar = null;
            MasterUser = null;

            foreach (var f in result.Files.OrderBy(f => f.ServerFolderName).ThenBy(f => f.ProfileFolderName)
                         .ThenBy(f => f.Kind).ThenBy(f => f.Id))
            {
                if (f.Kind == SettingsFileKind.Char)
                    CharFiles.Add(f);
                else
                    UserFiles.Add(f);

                TargetRows.Add(new SelectableFileRow(f));
            }

            if (result.Warnings.Count > 0)
                StatusMessage = string.Join(" ", result.Warnings) + $" Found {result.Files.Count} file(s).";
            else
                StatusMessage = $"Found {result.Files.Count} file(s).";

            await _settingsStore.SaveAsync(new AppSettings { LastEveRootPath = EveRootPath });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        if (MasterChar is null || MasterUser is null)
        {
            StatusMessage = "Select both a master core_char file and a master core_user file.";
            return;
        }

        var targets = TargetRows.Where(r => r.IsSelected).Select(r => r.Entry).ToList();
        if (targets.Count == 0)
        {
            StatusMessage = "Select one or more target files using the checkboxes.";
            return;
        }

        if (_shell is null)
        {
            StatusMessage = "Window is not ready yet.";
            return;
        }

        var preview = string.Join(
            Environment.NewLine,
            targets.Select(t =>
                $"{t.ServerFolderName}\\{t.ProfileFolderName} — {t.Kind} {t.Id} ← {(t.Kind == SettingsFileKind.Char ? MasterChar.FileName : MasterUser.FileName)}"));

        var ok = await DialogHelper.ConfirmAsync(
            _shell,
            "Confirm overwrite",
            "Close EVE before continuing. Existing files will be backed up with a .bak-<timestamp> suffix next to each target." +
            Environment.NewLine + Environment.NewLine + preview);

        if (!ok)
            return;

        IsBusy = true;
        try
        {
            var results = _copyService.CopyWithBackups(MasterChar.FullPath, MasterUser.FullPath, targets);

            var errors = results.Where(r => !r.Succeeded).Select(r => $"{r.DestinationPath}: {r.Error}").ToList();
            StatusMessage = errors.Count == 0
                ? $"Done. Updated {results.Count(r => r.Succeeded)} file(s)."
                : $"Completed with errors: {string.Join("; ", errors)}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
