using AseAudit.DbTool.Services;
using AseAudit.DbTool.Tui;
using FluentAssertions;
using Moq;
using Xunit;

namespace AseAudit.DbTool.Tests.Tui;

public class ModeDetectorTests
{
    private static readonly string[] ManifestTables = { "Area", "Building" };

    [Fact]
    public void Cannot_connect_returns_NoConnection()
    {
        var conn = new Mock<ISqlServerConnector>();
        conn.Setup(c => c.CanConnect(It.IsAny<string>())).Returns(false);

        var mode = new ModeDetector(conn.Object)
            .Detect("master-cs", "audit-cs", "Ase_Audit", ManifestTables);

        mode.Should().Be(Mode.NoConnection);
    }

    [Fact]
    public void Db_not_exists_returns_Deploy()
    {
        var conn = new Mock<ISqlServerConnector>();
        conn.Setup(c => c.CanConnect("master-cs")).Returns(true);
        conn.Setup(c => c.DatabaseExists("master-cs", "Ase_Audit")).Returns(false);

        var mode = new ModeDetector(conn.Object)
            .Detect("master-cs", "audit-cs", "Ase_Audit", ManifestTables);

        mode.Should().Be(Mode.Deploy);
    }

    [Fact]
    public void Db_exists_but_no_manifest_tables_returns_Deploy()
    {
        var conn = new Mock<ISqlServerConnector>();
        conn.Setup(c => c.CanConnect("master-cs")).Returns(true);
        conn.Setup(c => c.DatabaseExists("master-cs", "Ase_Audit")).Returns(true);
        conn.Setup(c => c.AnyTableExists("audit-cs", ManifestTables)).Returns(false);

        var mode = new ModeDetector(conn.Object)
            .Detect("master-cs", "audit-cs", "Ase_Audit", ManifestTables);

        mode.Should().Be(Mode.Deploy);
    }

    [Fact]
    public void Db_exists_with_manifest_tables_returns_Dev()
    {
        var conn = new Mock<ISqlServerConnector>();
        conn.Setup(c => c.CanConnect("master-cs")).Returns(true);
        conn.Setup(c => c.DatabaseExists("master-cs", "Ase_Audit")).Returns(true);
        conn.Setup(c => c.AnyTableExists("audit-cs", ManifestTables)).Returns(true);

        var mode = new ModeDetector(conn.Object)
            .Detect("master-cs", "audit-cs", "Ase_Audit", ManifestTables);

        mode.Should().Be(Mode.Dev);
    }
}
