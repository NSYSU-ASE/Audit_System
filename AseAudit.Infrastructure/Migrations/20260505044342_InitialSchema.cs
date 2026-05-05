using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AseAudit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FireWallRule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    HostName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MACAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Profile = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Direction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocalPort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RemotePort = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Protocol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DestinationIP = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FireWallRule", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Identification_AM_Account",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    HostName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MACAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    AccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordRequired = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identification_AM_Account", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Identification_AM_rule",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    HostName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MACAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    RestrictAnonymousSAM = table.Column<bool>(type: "bit", nullable: false),
                    EveryoneIncludesAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    RestrictAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    UserDomain = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DomainRole = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identification_AM_rule", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FireWallRule_HostName",
                table: "FireWallRule",
                column: "HostName");

            migrationBuilder.CreateIndex(
                name: "IX_Identification_AM_Account_HostName",
                table: "Identification_AM_Account",
                column: "HostName");

            migrationBuilder.CreateIndex(
                name: "IX_Identification_AM_rule_HostName",
                table: "Identification_AM_rule",
                column: "HostName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FireWallRule");

            migrationBuilder.DropTable(
                name: "Identification_AM_Account");

            migrationBuilder.DropTable(
                name: "Identification_AM_rule");
        }
    }
}
