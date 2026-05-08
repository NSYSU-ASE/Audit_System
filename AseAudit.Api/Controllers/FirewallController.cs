using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;

namespace AseAudit.Api.Controllers;

[ApiController]
[Route("api/firewall")]
public class FirewallController : ControllerBase
{
    private readonly IDbConnection _db;

    public FirewallController(IDbConnection db)
    {
        _db = db;
    }

    [HttpGet("audit")]
    public IActionResult Audit([FromQuery] string? hostName = null)
    {
        if (!FireWallRuleTableExists())
        {
            return Ok(new
            {
                module = "Firewall",
                hostName = hostName ?? "--",
                averageScore = (double?)null,
                includedRuleCount = 0,
                totalRuleCount = 0,
                note = "dbo.FireWallRule 尚未建立，請先執行 FireWallRule schema / data SQL。",
                rules = Array.Empty<object>()
            });
        }

        var rows = GetRows(hostName).ToList();
        if (rows.Count == 0)
        {
            return Ok(new
            {
                module = "Firewall",
                hostName = hostName ?? "--",
                averageScore = (double?)null,
                includedRuleCount = 0,
                totalRuleCount = 0,
                note = "FireWallRule 沒有符合條件的資料。",
                rules = Array.Empty<object>()
            });
        }

        var hosts = rows
            .GroupBy(x => x.HostName)
            .Select(group =>
            {
                var ruleRows = group.ToList();
                var rules = new[]
                {
                    EvaluateUntrustedNetworkAccess(ruleRows),
                    EvaluateBoundaryProtection(ruleRows),
                    EvaluatePersonToPersonCommunication(ruleRows)
                };

                var avg = Math.Round(rules.Average(x => x.Score), 2);

                return new
                {
                    hostName = group.Key,
                    totalFirewallRules = ruleRows.Count,
                    averageScore = avg,
                    includedRuleCount = rules.Length,
                    totalRuleCount = rules.Length,
                    rules
                };
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(hostName))
        {
            var host = hosts.FirstOrDefault();
            if (host is not null)
            {
                return Ok(host);
            }

            return Ok(new
            {
                hostName,
                totalFirewallRules = 0,
                averageScore = (double?)null,
                includedRuleCount = 0,
                totalRuleCount = 0,
                rules = Array.Empty<FirewallAuditRuleRow>()
            });
        }

        return Ok(new
        {
            module = "Firewall",
            hostCount = hosts.Count,
            averageScore = hosts.Count == 0
                ? (double?)null
                : Math.Round(hosts.Average(x => x.averageScore), 2),
            hosts
        });
    }

    private bool FireWallRuleTableExists()
    {
        const string sql = "SELECT CASE WHEN OBJECT_ID(N'dbo.FireWallRule', N'U') IS NULL THEN 0 ELSE 1 END";
        return _db.ExecuteScalar<int>(sql) == 1;
    }

    private IEnumerable<FireWallRuleRow> GetRows(string? hostName)
    {
        const string sql = @"
SELECT
    ID AS Id,
    CreatedTime,
    HostName,
    MACAddress,
    RuleName,
    DisplayName,
    Status,
    Profile,
    Action,
    Direction,
    LocalPort,
    RemotePort,
    Protocol,
    SourceIP,
    DestinationIP
FROM dbo.FireWallRule
WHERE @HostName IS NULL OR HostName = @HostName
ORDER BY HostName, ID;";

        return _db.Query<FireWallRuleRow>(
            sql,
            new { HostName = string.IsNullOrWhiteSpace(hostName) ? null : hostName });
    }

    private static FirewallAuditRuleRow EvaluateUntrustedNetworkAccess(List<FireWallRuleRow> rows)
    {
        var risky = rows
            .Where(IsEnabledAllowInbound)
            .Where(x => IsAnyAddress(x.DestinationIP) || IsAnyAddress(x.SourceIP))
            .Where(x => IsPublicOrAnyProfile(x.Profile) || IsSensitivePort(x.LocalPort))
            .ToList();

        var score = risky.Count == 0
            ? 100
            : risky.Count <= 3
                ? 70
                : risky.Count <= 10
                    ? 40
                    : 0;

        return new FirewallAuditRuleRow
        {
            SrMapping = "SR1.13 / SR1.13RE(1)",
            RuleName = "經未受信任網路之存取",
            Score = score,
            IncludedInScore = true,
            Status = score >= 80 ? "通過" : "未通過",
            Reason = risky.Count == 0
                ? "未發現啟用中的高風險對外開放 inbound allow 規則。"
                : $"發現 {risky.Count} 筆啟用中的 inbound allow 規則對 Any/Public 或敏感埠開放。",
            Evidence = risky.Take(8).Select(FormatRule).ToList()
        };
    }

    private static FirewallAuditRuleRow EvaluateBoundaryProtection(List<FireWallRuleRow> rows)
    {
        var enabledInbound = rows.Where(IsEnabledInbound).ToList();
        var allowInbound = enabledInbound
            .Where(x => IsAllow(x.Action))
            .ToList();
        var blockInbound = enabledInbound
            .Where(x => IsBlock(x.Action))
            .ToList();

        var openAnyInbound = allowInbound
            .Where(x => IsAnyAddress(x.DestinationIP) || IsAnyAddress(x.SourceIP))
            .ToList();

        var score = enabledInbound.Count == 0
            ? 0
            : openAnyInbound.Count == 0 && blockInbound.Count > 0
                ? 100
                : openAnyInbound.Count <= 5
                    ? 70
                    : 40;

        return new FirewallAuditRuleRow
        {
            SrMapping = "SR5.2 / SR5.2RE",
            RuleName = "安全區邊界保護 / 原則禁止例外允許",
            Score = score,
            IncludedInScore = true,
            Status = score >= 80 ? "通過" : "未通過",
            Reason = $"啟用 inbound 規則 {enabledInbound.Count} 筆；Allow {allowInbound.Count} 筆；Block {blockInbound.Count} 筆；Any 開放 {openAnyInbound.Count} 筆。",
            Evidence = openAnyInbound.Take(8).Select(FormatRule).ToList()
        };
    }

    private static FirewallAuditRuleRow EvaluatePersonToPersonCommunication(List<FireWallRuleRow> rows)
    {
        var keywords = new[]
        {
            "spotify", "xbox", "game bar", "solitaire", "clipchamp",
            "teams", "skype", "discord", "line", "messenger", "whatsapp"
        };

        var consumerRules = rows
            .Where(x => IsEnabled(x.Status) && IsAllow(x.Action))
            .Where(x =>
            {
                var text = $"{x.RuleName} {x.DisplayName}".ToLowerInvariant();
                return keywords.Any(text.Contains);
            })
            .ToList();

        var score = consumerRules.Count == 0
            ? 100
            : consumerRules.Count <= 3
                ? 60
                : 30;

        return new FirewallAuditRuleRow
        {
            SrMapping = "SR5.3",
            RuleName = "通用個人對個人之通訊限制",
            Score = score,
            IncludedInScore = true,
            Status = score >= 80 ? "通過" : "未通過",
            Reason = consumerRules.Count == 0
                ? "未發現常見個人通訊 / 消費型 App 的啟用允許規則。"
                : $"發現 {consumerRules.Count} 筆常見個人通訊或消費型 App 的啟用允許規則。",
            Evidence = consumerRules.Take(8).Select(FormatRule).ToList()
        };
    }

    private static bool IsEnabledAllowInbound(FireWallRuleRow row)
        => IsEnabledInbound(row) && IsAllow(row.Action);

    private static bool IsEnabledInbound(FireWallRuleRow row)
        => IsEnabled(row.Status)
           && string.Equals(row.Direction, "Inbound", StringComparison.OrdinalIgnoreCase);

    private static bool IsEnabled(string? value)
        => string.Equals(value, "True", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "Enabled", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllow(string? value)
        => string.Equals(value, "Allow", StringComparison.OrdinalIgnoreCase);

    private static bool IsBlock(string? value)
        => string.Equals(value, "Block", StringComparison.OrdinalIgnoreCase);

    private static bool IsAnyAddress(string? value)
    {
        var text = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(text)
               || text.Equals("Any", StringComparison.OrdinalIgnoreCase)
               || text.Equals("0.0.0.0/0", StringComparison.OrdinalIgnoreCase)
               || text.Equals("::/0", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicOrAnyProfile(string? value)
    {
        var text = value ?? "";
        return text.Contains("Public", StringComparison.OrdinalIgnoreCase)
               || text.Contains("Any", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSensitivePort(string? value)
    {
        var text = value ?? "";
        var sensitive = new[] { "135", "139", "445", "3389", "5985", "5986", "5001", "7079", "8080", "8443" };
        return sensitive.Any(port => text.Split(',', StringSplitOptions.TrimEntries).Contains(port));
    }

    private static string FormatRule(FireWallRuleRow row)
        => $"{row.DisplayName ?? row.RuleName}｜{row.Direction}/{row.Action}｜Profile={row.Profile ?? "--"}｜LocalPort={row.LocalPort ?? "--"}｜Remote={row.DestinationIP ?? "--"}";
}

public sealed class FireWallRuleRow
{
    public int Id { get; set; }
    public DateTime CreatedTime { get; set; }
    public string HostName { get; set; } = "";
    public string? MACAddress { get; set; }
    public string RuleName { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Status { get; set; }
    public string? Profile { get; set; }
    public string? Action { get; set; }
    public string? Direction { get; set; }
    public string? LocalPort { get; set; }
    public string? RemotePort { get; set; }
    public string? Protocol { get; set; }
    public string? SourceIP { get; set; }
    public string? DestinationIP { get; set; }
}

public sealed class FirewallAuditRuleRow
{
    public string SrMapping { get; set; } = "";
    public string RuleName { get; set; } = "";
    public int Score { get; set; }
    public bool IncludedInScore { get; set; }
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public List<string> Evidence { get; set; } = new();
}
