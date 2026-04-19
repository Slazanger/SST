using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EveSettings.Core;
using EveSettings.Core.Models;
using EveSettings.Services;

namespace EveSettings.ViewModels;

public partial class InspectorViewModel : ViewModelBase
{
    private readonly IDatDecoder _decoder = new NoOpDatDecoder();
    private Window? _shell;

    [ObservableProperty] private string? _currentPath;
    [ObservableProperty] private long _fileSizeBytes;
    [ObservableProperty] private string _inferredKind = "Unknown";
    [ObservableProperty] private DateTime? _modifiedUtc;
    [ObservableProperty] private string _decoderNote =
        "NoOpDatDecoder: semantic keys are not available yet. Use Hex / Strings / Diff for inspection.";

    [ObservableProperty] private string? _diffPathA;
    [ObservableProperty] private string? _diffPathB;
    [ObservableProperty] private string _diffSummary = "Pick two captures of the same file and run diff.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<string> HexLines { get; } = [];
    public ObservableCollection<ExtractedString> Strings { get; } = [];
    public ObservableCollection<DatRecord> Records { get; } = [];

    private byte[]? _bytes;

    public void AttachShell(Window shell) => _shell = shell;

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (_shell is null)
        {
            return;
        }

        var files = await _shell.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open a .dat file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("EVE settings") { Patterns = ["*.dat"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        var file = files.FirstOrDefault();
        if (file is null)
            return;

        try
        {
            var path = file.Path.LocalPath;
            await LoadFromPathAsync(path);
        }
        catch (Exception ex)
        {
            DecoderNote = ex.Message;
        }
    }

    public async Task LoadFromPathAsync(string path)
    {
        IsBusy = true;
        try
        {
            await Task.Yield();
            _bytes = DatFileReader.ReadFile(path);
            CurrentPath = path;
            FileSizeBytes = _bytes.LongLength;
            ModifiedUtc = File.GetLastWriteTimeUtc(path);

            var name = Path.GetFileName(path) ?? path;
            InferredKind = name.StartsWith("core_char_", StringComparison.OrdinalIgnoreCase)
                ? "core_char"
                : name.StartsWith("core_user_", StringComparison.OrdinalIgnoreCase)
                    ? "core_user"
                    : "Unknown";

            HexLines.Clear();
            foreach (var line in HexFormatter.FormatLines(_bytes))
                HexLines.Add(line);

            Strings.Clear();
            foreach (var s in StringExtractor.Extract(_bytes))
                Strings.Add(s);

            Records.Clear();
            foreach (var r in _decoder.Decode(_bytes, name))
                Records.Add(r);

            DiffSummary = "Pick two captures of the same file and run diff.";
            DecoderNote =
                "NoOpDatDecoder: semantic keys are not available yet. Use Hex / Strings / Diff for inspection.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PickDiffAAsync()
    {
        var p = await PickDatPathAsync("Select file A");
        if (p is not null)
            DiffPathA = p;
    }

    [RelayCommand]
    private async Task PickDiffBAsync()
    {
        var p = await PickDatPathAsync("Select file B");
        if (p is not null)
            DiffPathB = p;
    }

    [RelayCommand]
    private Task RunDiffAsync()
    {
        if (string.IsNullOrWhiteSpace(DiffPathA) || string.IsNullOrWhiteSpace(DiffPathB))
        {
            DiffSummary = "Select both file A and file B.";
            return Task.CompletedTask;
        }

        try
        {
            var a = DatFileReader.ReadFile(DiffPathA);
            var b = DatFileReader.ReadFile(DiffPathB);

            if (a.Length != b.Length)
            {
                DiffSummary =
                    $"Warning: lengths differ (A={a.Length}, B={b.Length}). Comparing the shared prefix only.";
            }
            else
            {
                DiffSummary = "Lengths match. Comparing full buffers.";
            }

            var segments = BinaryDiffer.ComparePrefix(a, b);
            Records.Clear();
            foreach (var r in DatRecordEstimator.FromDiffSegments(segments))
                Records.Add(r);

            DiffSummary += $" Found {segments.Count} changed region(s). See Records tab.";
        }
        catch (Exception ex)
        {
            DiffSummary = ex.Message;
        }

        return Task.CompletedTask;
    }

    private async Task<string?> PickDatPathAsync(string title)
    {
        if (_shell is null)
            return null;

        var files = await _shell.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("EVE settings") { Patterns = ["*.dat"] },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }
}
