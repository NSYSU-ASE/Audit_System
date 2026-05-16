namespace AseAudit.DbTool.Manifest;

public sealed record TableEntry(
    string Name,
    int LoadOrder,
    bool Backupable,
    string CreateScript,
    string? Description = null);
