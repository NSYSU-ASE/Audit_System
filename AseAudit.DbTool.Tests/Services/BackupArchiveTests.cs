using AseAudit.DbTool.Services;
using FluentAssertions;
using Xunit;

namespace AseAudit.DbTool.Tests.Services;

public class BackupArchiveTests : IDisposable
{
    private readonly string _tempDir;

    public BackupArchiveTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void CreateUserBackupFolder_creates_timestamped_folder()
    {
        var archive = new BackupArchive(_tempDir);
        var path = archive.CreateUserBackupFolder(new DateTime(2026, 5, 13, 15, 30, 22));
        path.Should().EndWith("2026-05-13-153022");
        Directory.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void CreateAutoBackupFolder_uses_prefix()
    {
        var archive = new BackupArchive(_tempDir);
        var path = archive.CreateAutoBackupFolder(new DateTime(2026, 5, 13, 15, 30, 22));
        Path.GetFileName(path).Should().Be("_autoBackup-2026-05-13-153022");
        Directory.Exists(path).Should().BeTrue();
    }

    [Fact]
    public void RotateAutoBackups_keeps_only_N_newest()
    {
        var archive = new BackupArchive(_tempDir);
        for (int i = 1; i <= 7; i++)
        {
            var dt = new DateTime(2026, 5, 13, 10, 0, i);
            archive.CreateAutoBackupFolder(dt);
        }

        archive.RotateAutoBackups(keep: 5);

        var remaining = Directory.GetDirectories(_tempDir)
            .Select(Path.GetFileName)
            .Where(n => n!.StartsWith("_autoBackup-"))
            .OrderBy(n => n)
            .ToList();

        remaining.Should().HaveCount(5);
        remaining.Should().Contain("_autoBackup-2026-05-13-100003");
        remaining.Should().Contain("_autoBackup-2026-05-13-100007");
        remaining.Should().NotContain("_autoBackup-2026-05-13-100001");
        remaining.Should().NotContain("_autoBackup-2026-05-13-100002");
    }

    [Fact]
    public void RotateAutoBackups_does_not_touch_user_backups()
    {
        var archive = new BackupArchive(_tempDir);
        archive.CreateUserBackupFolder(new DateTime(2026, 5, 13, 10, 0, 0));
        for (int i = 1; i <= 6; i++)
            archive.CreateAutoBackupFolder(new DateTime(2026, 5, 13, 11, 0, i));

        archive.RotateAutoBackups(keep: 5);

        Directory.GetDirectories(_tempDir).Should()
            .Contain(d => Path.GetFileName(d) == "2026-05-13-100000");
    }

    [Fact]
    public void ListUserBackupFolders_excludes_autoBackups()
    {
        var archive = new BackupArchive(_tempDir);
        archive.CreateUserBackupFolder(new DateTime(2026, 5, 13, 10, 0, 0));
        archive.CreateAutoBackupFolder(new DateTime(2026, 5, 13, 11, 0, 0));

        var list = archive.ListUserBackupFolders();
        list.Should().HaveCount(1);
        list[0].Should().EndWith("2026-05-13-100000");
    }
}
