using AseAudit.DbTool.Manifest;
using FluentAssertions;
using Xunit;

namespace AseAudit.DbTool.Tests.Manifest;

public class ManifestLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public ManifestLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"manifest-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "sql"));
        File.WriteAllText(Path.Combine(_tempDir, "sql", "a.sql"), "-- a");
        File.WriteAllText(Path.Combine(_tempDir, "sql", "b.sql"), "-- b");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteManifest(string json)
    {
        var path = Path.Combine(_tempDir, "schema-manifest.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Load_valid_manifest_returns_parsed_object()
    {
        var json = """
        {
          "version": "1.0",
          "database": "TestDb",
          "scriptRoot": "sql",
          "tables": [
            { "name": "A", "loadOrder": 10, "backupable": true, "createScript": "a.sql" },
            { "name": "B", "loadOrder": 20, "backupable": false, "createScript": "b.sql" }
          ]
        }
        """;
        var path = WriteManifest(json);

        var manifest = new ManifestLoader().Load(path);

        manifest.Database.Should().Be("TestDb");
        manifest.Tables.Should().HaveCount(2);
        manifest.Tables[0].Name.Should().Be("A");
        manifest.Tables[0].LoadOrder.Should().Be(10);
        manifest.Tables[1].Backupable.Should().BeFalse();
    }

    [Fact]
    public void Load_missing_file_throws()
    {
        var act = () => new ManifestLoader().Load(Path.Combine(_tempDir, "nope.json"));
        act.Should().Throw<ManifestException>()
           .WithMessage("找不到 manifest 檔*");
    }

    [Fact]
    public void Load_invalid_json_throws()
    {
        var path = WriteManifest("{ not json");
        var act = () => new ManifestLoader().Load(path);
        act.Should().Throw<ManifestException>()
           .WithMessage("manifest JSON 格式錯誤*");
    }

    [Fact]
    public void Load_empty_tables_throws()
    {
        var json = """{"version":"1.0","database":"X","scriptRoot":"sql","tables":[]}""";
        var path = WriteManifest(json);
        var act = () => new ManifestLoader().Load(path);
        act.Should().Throw<ManifestException>()
           .WithMessage("manifest 至少需有一張表");
    }

    [Fact]
    public void Load_duplicate_name_throws()
    {
        var json = """
        {"version":"1.0","database":"X","scriptRoot":"sql","tables":[
          {"name":"A","loadOrder":10,"backupable":true,"createScript":"a.sql"},
          {"name":"A","loadOrder":20,"backupable":true,"createScript":"b.sql"}
        ]}
        """;
        var path = WriteManifest(json);
        var act = () => new ManifestLoader().Load(path);
        act.Should().Throw<ManifestException>()
           .WithMessage("表名重複：A");
    }

    [Fact]
    public void Load_duplicate_loadOrder_throws()
    {
        var json = """
        {"version":"1.0","database":"X","scriptRoot":"sql","tables":[
          {"name":"A","loadOrder":10,"backupable":true,"createScript":"a.sql"},
          {"name":"B","loadOrder":10,"backupable":true,"createScript":"b.sql"}
        ]}
        """;
        var path = WriteManifest(json);
        var act = () => new ManifestLoader().Load(path);
        act.Should().Throw<ManifestException>()
           .WithMessage("loadOrder 重複：10*");
    }

    [Fact]
    public void Load_unsafe_name_throws()
    {
        var json = """
        {"version":"1.0","database":"X","scriptRoot":"sql","tables":[
          {"name":"A;DROP","loadOrder":10,"backupable":true,"createScript":"a.sql"}
        ]}
        """;
        var path = WriteManifest(json);
        var act = () => new ManifestLoader().Load(path);
        act.Should().Throw<ManifestException>()
           .WithMessage("表名含不合法字元*");
    }

    [Fact]
    public void Load_missing_create_script_throws()
    {
        var json = """
        {"version":"1.0","database":"X","scriptRoot":"sql","tables":[
          {"name":"A","loadOrder":10,"backupable":true,"createScript":"nope.sql"}
        ]}
        """;
        var path = WriteManifest(json);
        var act = () => new ManifestLoader().Load(path);
        act.Should().Throw<ManifestException>()
           .WithMessage("*createScript 不存在*");
    }
}
