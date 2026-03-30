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
    /// SR1.2 授權軟體：比對安裝清單 vs 白名單，若有未授權軟體則不合格
    /// </summary>
    public sealed class AuthorizedSoftwareRule
    {
        public AuditItemResult Evaluate(
            IEnumerable<InstalledProgramRecordDto> installedPrograms,
            IEnumerable<AuthorizedSoftwareWhitelistEntryDto> whitelist)
        {
            var installed = (installedPrograms ?? Enumerable.Empty<InstalledProgramRecordDto>())
                .Where(p => !string.IsNullOrWhiteSpace(p.DisplayName))
                .Select(p => new InstalledProgramRecordDto
                {
                    DisplayName = Normalize(p.DisplayName),
                    Publisher = Normalize(p.Publisher),
                    DisplayVersion = Normalize(p.DisplayVersion)
                })
                .ToList();

            var allow = (whitelist ?? Enumerable.Empty<AuthorizedSoftwareWhitelistEntryDto>())
                .Where(w => !string.IsNullOrWhiteSpace(w.Name))
                .Select(w => new AuthorizedSoftwareWhitelistEntryDto
                {
                    Name = Normalize(w.Name),
                    Publisher = Normalize(w.Publisher),
                    VersionPrefix = Normalize(w.VersionPrefix),
                    MatchMode = string.IsNullOrWhiteSpace(w.MatchMode) ? "Contains" : w.MatchMode.Trim()
                })
                .ToList();

            if (installed.Count == 0)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.authorized_software",
                    Title = "授權軟體清單比對（SR1.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "未取得已安裝軟體清單，無法進行白名單比對。"
                };
            }

            if (allow.Count == 0)
            {
                return new AuditItemResult
                {
                    ItemKey = "software.authorized_software",
                    Title = "授權軟體清單比對（SR1.2）",
                    Score = 0,
                    Weight = 1,
                    Message = "未提供授權軟體白名單，無法判定是否存在未授權軟體。"
                };
            }

            var unauthorized = installed
                .Where(p => !IsAllowed(p, allow))
                .Select(p => p.DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (unauthorized.Count > 0)
            {
                var msg = new StringBuilder();
                msg.AppendLine("偵測到未授權/不在白名單的應用程式：");
                foreach (var name in unauthorized.Take(20))
                    msg.AppendLine($"- {name}");
                if (unauthorized.Count > 20)
                    msg.AppendLine($"...（共 {unauthorized.Count} 筆，僅顯示前 20 筆）");

                return new AuditItemResult
                {
                    ItemKey = "software.authorized_software",
                    Title = "授權軟體清單比對（SR1.2）",
                    Score = 0,
                    Weight = 1,
                    Message = msg.ToString().Trim()
                };
            }

            return new AuditItemResult
            {
                ItemKey = "software.authorized_software",
                Title = "授權軟體清單比對（SR1.2）",
                Score = 100,
                Weight = 1,
                Message = "已安裝軟體皆符合白名單，未發現未授權應用程式。"
            };
        }

        private static bool IsAllowed(
            InstalledProgramRecordDto program,
            List<AuthorizedSoftwareWhitelistEntryDto> allow)
        {
            foreach (var w in allow)
            {
                if (!string.IsNullOrWhiteSpace(w.Publisher))
                {
                    if (string.IsNullOrWhiteSpace(program.Publisher)) continue;
                    if (!program.Publisher.Contains(w.Publisher, StringComparison.OrdinalIgnoreCase)) continue;
                }

                if (string.Equals(w.MatchMode, "Exact", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(program.DisplayName, w.Name, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                else
                {
                    if (!program.DisplayName.Contains(w.Name, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(w.VersionPrefix))
                {
                    if (string.IsNullOrWhiteSpace(program.DisplayVersion)) continue;
                    if (!program.DisplayVersion.StartsWith(w.VersionPrefix, StringComparison.OrdinalIgnoreCase)) continue;
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
    }
}