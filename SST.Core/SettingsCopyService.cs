using SST.Core.Models;

namespace SST.Core;

public sealed class SettingsCopyService
{
    /// <summary>
    /// Copies master char/user bytes to each destination, backing up existing files first.
    /// </summary>
    /// <param name="masterCharPath">Source path for <c>core_char_*.dat</c> replacements.</param>
    /// <param name="masterUserPath">Source path for <c>core_user_*.dat</c> replacements.</param>
    /// <param name="targets">Destination entries to overwrite.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Per-destination results.</returns>
    public IReadOnlyList<CopyOperationResult> CopyWithBackups(
        string masterCharPath,
        string masterUserPath,
        IReadOnlyList<SettingsFileEntry> targets,
        CancellationToken ct = default)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var list = new List<CopyOperationResult>(targets.Count);

        foreach (var target in targets)
        {
            ct.ThrowIfCancellationRequested();

            var source = target.Kind == SettingsFileKind.Char ? masterCharPath : masterUserPath;
            var r = new CopyOperationResult(target.FullPath, source, target.Kind);
            list.Add(r);

            try
            {
                if (!File.Exists(source))
                {
                    r.Error = $"Source file does not exist: {source}";
                    continue;
                }

                if (File.Exists(target.FullPath))
                {
                    var backupPath = $"{target.FullPath}.bak-{stamp}";
                    File.Copy(target.FullPath, backupPath, overwrite: false);
                    r.BackupPath = backupPath;
                }

                File.Copy(source, target.FullPath, overwrite: true);
                r.Succeeded = true;
            }
            catch (Exception ex)
            {
                r.Error = ex.Message;
            }
        }

        return list;
    }
}

public sealed class CopyOperationResult(string destinationPath, string sourcePath, SettingsFileKind targetKind)
{
    public string DestinationPath { get; } = destinationPath;
    public string SourcePath { get; } = sourcePath;
    public SettingsFileKind TargetKind { get; } = targetKind;
    public string? BackupPath { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}
