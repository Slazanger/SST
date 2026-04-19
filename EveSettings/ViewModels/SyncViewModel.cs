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
    private readonly EsiUniverseNamesClient _esiNames = new();
    private Window? _shell;

    private List<SettingsFileEntry> _allFiles = [];
    private string? _pendingServerFolder;
    private bool _suppressServerSelectionEvent;

    [ObservableProperty] private string? _eveRootPath;
    [ObservableProperty] private string _statusMessage = "Select your EVE local settings folder, then scan.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private CharacterNamedFileItem? _masterChar;
    [ObservableProperty] private CharacterNamedFileItem? _masterUser;
    [ObservableProperty] private ServerListItem? _selectedServer;

    public ObservableCollection<ServerListItem> Servers { get; } = [];
    public ObservableCollection<CharacterNamedFileItem> CharFiles { get; } = [];
    public ObservableCollection<CharacterNamedFileItem> UserFiles { get; } = [];
    public ObservableCollection<SelectableFileRow> TargetRows { get; } = [];

    public void AttachShell(Window shell) => _shell = shell;

    public async Task InitializeAsync()
    {
        var s = await _settingsStore.LoadAsync();
        EveRootPath = s.LastEveRootPath;
        _pendingServerFolder = s.LastServerFolderName;

        if (string.IsNullOrWhiteSpace(EveRootPath))
            EveRootPath = EvePaths.GetDefaultWindowsCcpEveRoot();

        if (Directory.Exists(EveRootPath))
            await ScanAsync();
    }

    private AppSettings CaptureSettings() => new()
    {
        LastEveRootPath = EveRootPath,
        LastServerFolderName = SelectedServer?.FolderName,
    };

    partial void OnSelectedServerChanged(ServerListItem? value)
    {
        if (_suppressServerSelectionEvent || value is null || _allFiles.Count == 0)
            return;

        _ = ApplyServerFilterAndResolveNamesAsync();
        _ = _settingsStore.SaveAsync(CaptureSettings());
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
        await _settingsStore.SaveAsync(CaptureSettings());
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
            _allFiles = result.Files.ToList();

            MasterChar = null;
            MasterUser = null;
            CharFiles.Clear();
            UserFiles.Clear();
            TargetRows.Clear();
            Servers.Clear();

            var distinctServers = _allFiles
                .Select(f => f.ServerFolderName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => EveServerLabels.GetDisplayLabel(n))
                .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _suppressServerSelectionEvent = true;
            SelectedServer = null;
            foreach (var name in distinctServers)
                Servers.Add(new ServerListItem(name));

            if (Servers.Count == 0)
            {
                SelectedServer = null;
            }
            else
            {
                var match = !string.IsNullOrWhiteSpace(_pendingServerFolder)
                    ? Servers.FirstOrDefault(s =>
                        string.Equals(s.FolderName, _pendingServerFolder, StringComparison.OrdinalIgnoreCase))
                    : null;

                SelectedServer = match ?? Servers[0];
                _pendingServerFolder = null;
            }

            _suppressServerSelectionEvent = false;

            if (SelectedServer is not null)
                await ApplyServerFilterAndResolveNamesAsync();
            else if (Servers.Count == 0)
                StatusMessage = result.Warnings.Count > 0
                    ? string.Join(" ", result.Warnings) + $" Found {result.Files.Count} file(s)."
                    : $"Found {result.Files.Count} file(s); no server folders detected.";

            if (result.Warnings.Count > 0 && SelectedServer is not null)
                StatusMessage = string.Join(" ", result.Warnings) + " " + StatusMessage;

            await _settingsStore.SaveAsync(CaptureSettings());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyServerFilterAndResolveNamesAsync()
    {
        if (SelectedServer is null)
            return;

        var folder = SelectedServer.FolderName;
        var filtered = _allFiles
            .Where(f => string.Equals(f.ServerFolderName, folder, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.ProfileFolderName)
            .ThenBy(f => f.Kind)
            .ThenBy(f => f.Id)
            .ToList();

        MasterChar = null;
        MasterUser = null;
        CharFiles.Clear();
        UserFiles.Clear();
        TargetRows.Clear();

        foreach (var f in filtered)
        {
            if (f.Kind == SettingsFileKind.Char)
                CharFiles.Add(new CharacterNamedFileItem(f));
            else
                UserFiles.Add(new CharacterNamedFileItem(f));

            TargetRows.Add(new SelectableFileRow(f));
        }

        await RefreshNamesAsync(EveFolderEsiRouting.GetClusterForFolder(folder));
    }

    private async Task RefreshNamesAsync(EsiCluster cluster)
    {
        // core_user_*.dat IDs are account-level, not ESI "character" entities — only resolve core_char IDs.
        foreach (var item in UserFiles)
            item.ResolvedName = null;

        var idSet = new HashSet<long>();
        foreach (var item in CharFiles)
        {
            if (long.TryParse(item.Entry.Id, out var id))
                idSet.Add(id);
        }

        foreach (var row in TargetRows.Where(r => r.Entry.Kind == SettingsFileKind.Char))
        {
            if (long.TryParse(row.Entry.Id, out var id))
                idSet.Add(id);
        }

        foreach (var row in TargetRows.Where(r => r.Entry.Kind == SettingsFileKind.User))
            row.ResolvedName = null;

        var distinct = idSet.ToList();
        if (distinct.Count == 0)
        {
            StatusMessage =
                $"Server: {EveServerLabels.GetDisplayLabel(SelectedServer!.FolderName)} — no core_char IDs to look up.";
            return;
        }

        try
        {
            var map = await _esiNames.ResolveCharacterNamesAsync(distinct, cluster).ConfigureAwait(true);

            foreach (var item in CharFiles)
            {
                if (long.TryParse(item.Entry.Id, out var id) && map.TryGetValue(id, out var name))
                    item.ResolvedName = name;
            }

            foreach (var row in TargetRows.Where(r => r.Entry.Kind == SettingsFileKind.Char))
            {
                if (long.TryParse(row.Entry.Id, out var id) && map.TryGetValue(id, out var name))
                    row.ResolvedName = name;
            }

            var resolved = distinct.Count(id => map.ContainsKey(id));
            StatusMessage =
                $"Server: {EveServerLabels.GetDisplayLabel(SelectedServer!.FolderName)} — resolved {resolved} character name(s) from {distinct.Count} core_char ID(s) via ESI.";
        }
        catch (Exception ex)
        {
            StatusMessage =
                $"Server: {EveServerLabels.GetDisplayLabel(SelectedServer!.FolderName)} — could not resolve character names ({ex.Message}). core_user rows stay as Account (id).";
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
            TargetRows.Where(r => r.IsSelected).Select(r =>
                $"{r.DisplayLine} ← {(r.Entry.Kind == SettingsFileKind.Char ? MasterChar.DisplayLine : MasterUser.DisplayLine)}"));

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
            var results = _copyService.CopyWithBackups(MasterChar.Entry.FullPath, MasterUser.Entry.FullPath, targets);

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
