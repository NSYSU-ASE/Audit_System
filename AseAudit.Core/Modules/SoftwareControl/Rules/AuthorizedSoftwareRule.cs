using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Software.Dtos;

namespace AseAudit.Core.Modules.Software.Rules
{
    /// <summary>
    /// SR1.2 授權軟體：比對安裝清單 vs 黑名單，若有禁止軟體則不合格
    /// </summary>
    public sealed class AuthorizedSoftwareRule
    {
        public AuditItemResult Evaluate(
            IEnumerable<InstalledProgramRecordDto> installedPrograms,
            IEnumerable<AuthorizedSoftwareBlacklistEntryDto> blacklist)
        {
            var installed = (installedPrograms ?? Enumerable.Empty<InstalledProgramRecordDto>())
                .Where(p => !string.IsNullOrWhiteSpace(p.DisplayName))
                .Select(p => new InstalledProgramRecordDto
                {
                    DisplayName = NormalizeRequired(p.DisplayName),
                    Publisher = Normalize(p.Publisher),
                    DisplayVersion = Normalize(p.DisplayVersion)
                })
                .ToList();

            var deny = (blacklist ?? Enumerable.Empty<AuthorizedSoftwareBlacklistEntryDto>())
                .Where(b => !string.IsNullOrWhiteSpace(b.Name))
                .Select(b => new AuthorizedSoftwareBlacklistEntryDto
                {
                    Name = NormalizeRequired(b.Name),
                    Publisher = Normalize(b.Publisher),
                    VersionPrefix = Normalize(b.VersionPrefix),
                    MatchMode = string.IsNullOrWhiteSpace(b.MatchMode) ? "Contains" : b.MatchMode.Trim()
                })
                .ToList();

            if (installed.Count == 0)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.authorized_software",
                    Title = "禁止軟體黑名單比對（SR1.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "未取得已安裝軟體清單，無法進行黑名單比對。"
                };
            }

            if (deny.Count == 0)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.authorized_software",
                    Title = "禁止軟體黑名單比對（SR1.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "未提供禁止軟體黑名單，無法判定是否存在禁止或未授權軟體。"
                };
            }

            var blocked = installed
                .Where(p => IsBlocked(p, deny))
                .Select(p => p.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (blocked.Count > 0)
            {
                var msg = new StringBuilder();
                msg.AppendLine("偵測到禁止/未授權的黑名單應用程式：");
                foreach (var name in blocked.Take(20))
                    msg.AppendLine($"- {name}");
                if (blocked.Count > 20)
                    msg.AppendLine($"...（共 {blocked.Count} 筆，僅顯示前 20 筆）");

                return new AuditItemResult
                {
                    ItemKey = "software.authorized_software",
                    Title = "禁止軟體黑名單比對（SR1.2）",
                    Score = 0,
                    Weight = 1,
                    Message = msg.ToString().Trim()
                };
            }

            return new AuditItemResult
            {
                ItemKey = "software.authorized_software",
                Title = "禁止軟體黑名單比對（SR1.2）",
                Score = 100,
                Weight = 1,
                Message = "未偵測到黑名單內的禁止或未授權應用程式。"
            };
        }

        private static bool IsBlocked(
            InstalledProgramRecordDto program,
            List<AuthorizedSoftwareBlacklistEntryDto> deny)
        {
            foreach (var b in deny)
            {
                if (!string.IsNullOrWhiteSpace(b.Publisher))
                {
                    if (string.IsNullOrWhiteSpace(program.Publisher)) continue;
                    if (!program.Publisher.Contains(b.Publisher, StringComparison.OrdinalIgnoreCase)) continue;
                }

                if (string.Equals(b.MatchMode, "Exact", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(program.DisplayName, b.Name, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                else
                {
                    if (!program.DisplayName.Contains(b.Name, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(b.VersionPrefix))
                {
                    if (string.IsNullOrWhiteSpace(program.DisplayVersion)) continue;
                    if (!program.DisplayVersion.StartsWith(b.VersionPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                }

                return true;
            }

            return false;
        }

        private static string? Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            return input.Trim().Replace("\t", " ").Replace("  ", " ");
        }

        private static string NormalizeRequired(string input)
        {
            return Normalize(input) ?? "";
        }

    }
}
