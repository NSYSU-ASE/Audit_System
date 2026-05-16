namespace AseAudit.DbTool.Manifest;

public sealed record ManifestFile(
    string Version,
    string Database,
    string ScriptRoot,
    IReadOnlyList<TableEntry> Tables);
