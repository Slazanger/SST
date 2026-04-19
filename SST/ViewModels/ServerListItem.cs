using SST.Core;

namespace SST.ViewModels;

public sealed class ServerListItem(string folderName) : IEquatable<ServerListItem>
{
    public string FolderName { get; } = folderName;

    public string DisplayLabel => EveServerLabels.GetDisplayLabel(FolderName);

    public bool Equals(ServerListItem? other) =>
        other is not null && string.Equals(FolderName, other.FolderName, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as ServerListItem);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FolderName);
}
