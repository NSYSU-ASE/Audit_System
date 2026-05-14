namespace AseAudit.DbTool.Services;

public sealed class BackupArchive
{
    private const string AutoPrefix = "_autoBackup-";

    public string RootPath { get; }

    public BackupArchive(string rootPath)
    {
        RootPath = rootPath;
        Directory.CreateDirectory(RootPath);
    }

    public string CreateUserBackupFolder(DateTime timestamp)
    {
        var folder = Path.Combine(RootPath, timestamp.ToString("yyyy-MM-dd-HHmmss"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    public string CreateAutoBackupFolder(DateTime timestamp)
    {
        var folder = Path.Combine(RootPath, AutoPrefix + timestamp.ToString("yyyy-MM-dd-HHmmss"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    public void RotateAutoBackups(int keep)
    {
        if (keep < 0) throw new ArgumentOutOfRangeException(nameof(keep));

        var autoFolders = Directory
            .EnumerateDirectories(RootPath)
            .Where(d => Path.GetFileName(d).StartsWith(AutoPrefix, StringComparison.Ordinal))
            .OrderByDescending(d => Path.GetFileName(d))
            .ToList();

        foreach (var stale in autoFolders.Skip(keep))
            Directory.Delete(stale, recursive: true);
    }

    public IReadOnlyList<string> ListUserBackupFolders()
    {
        return Directory
            .EnumerateDirectories(RootPath)
            .Where(d => !Path.GetFileName(d).StartsWith(AutoPrefix, StringComparison.Ordinal))
            .OrderByDescending(d => Path.GetFileName(d))
            .ToList();
    }

    public IReadOnlyList<string> ListAllBackupFolders()
    {
        return Directory
            .EnumerateDirectories(RootPath)
            .OrderByDescending(d => Path.GetFileName(d))
            .ToList();
    }
}
