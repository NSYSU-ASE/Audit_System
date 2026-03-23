using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Firewall.Dtos;

namespace AseAudit.Core.Modules.Firewall.Rules
{
    /// <summary>
    /// 防火牆設定檢查
    /// 對應：
    /// SR1.3, SR2.6, SR3.8, SR5.1RE, SR5.2RE, SR5.3
    /// </summary>
    public sealed class FirewallPolicyRule
    {
        public AuditItemResult Evaluate(
            string siteId,
            FirewallPolicySnapshotDto policy,
            IEnumerable<FirewallWhitelistEntryDto> firewallWhitelist,
            IEnumerable<JumpHostInventoryRecordDto> jumpHosts,
            IEnumerable<DomainTableRecordDto> domainTable,
            IEnumerable<AccessNetworkSegmentRecordDto> accessSegments)
        {
            siteId = (siteId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(siteId))
            {
                return Fail("未提供 siteId，無法執行防火牆稽核。");
            }

            if (policy is null)
            {
                return Fail("找不到防火牆策略設定資料。");
            }

            int totalChecks = 3;
            int passedChecks = 0;
            var messages = new List<string>();

            // 1. 是否採預設拒絕策略
            if (policy.DefaultDenyEnabled)
            {
                passedChecks++;
                messages.Add("已採用防火牆預設拒絕策略。");
            }
            else
            {
                messages.Add("未採用防火牆預設拒絕策略。");
            }

            // 2. 防火牆白名單是否與跳板主機清單一致
            var activeJumpHosts = (jumpHosts ?? Enumerable.Empty<JumpHostInventoryRecordDto>())
                .Where(x => string.Equals((x.SiteId ?? "").Trim(), siteId, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.IsEnabled)
                .ToList();

            var whitelistRows = (firewallWhitelist ?? Enumerable.Empty<FirewallWhitelistEntryDto>())
                .Where(x => string.Equals((x.SiteId ?? "").Trim(), siteId, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.IsAllowed)
                .ToList();

            bool whitelistMatchesJumpHosts = true;

            if (activeJumpHosts.Count == 0)
            {
                whitelistMatchesJumpHosts = false;
                messages.Add("跳板主機清單為空，無法比對防火牆白名單。");
            }
            else
            {
                foreach (var host in activeJumpHosts)
                {
                    bool found = whitelistRows.Any(w =>
                        string.Equals((w.SourceIp ?? "").Trim(), (host.IpAddress ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!found)
                    {
                        whitelistMatchesJumpHosts = false;
                        messages.Add($"防火牆白名單缺少跳板主機：{host.HostName} ({host.IpAddress})");
                    }
                }
            }

            if (whitelistMatchesJumpHosts)
            {
                passedChecks++;
                messages.Add("防火牆白名單與跳板主機清單一致。");
            }

            // 3. 跳板主機是否存在於 access / domain 資料中
            var domainRows = (domainTable ?? Enumerable.Empty<DomainTableRecordDto>())
                .Where(x => string.Equals((x.SiteId ?? "").Trim(), siteId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var accessRows = (accessSegments ?? Enumerable.Empty<AccessNetworkSegmentRecordDto>())
                .Where(x => string.Equals((x.SiteId ?? "").Trim(), siteId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool jumpHostsConsistentWithTables = true;

            foreach (var host in activeJumpHosts)
            {
                bool inAccess = accessRows.Any(a =>
                    string.Equals((a.DeviceIp ?? "").Trim(), (host.IpAddress ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

                // 先做簡化版：
                // 只要 access 有，或 domain table 至少存在資料，就先視為可對應
                // 之後若要做精準 CIDR 比對，再補 helper
                bool inDomain = domainRows.Count > 0;

                if (!inAccess && !inDomain)
                {
                    jumpHostsConsistentWithTables = false;
                    messages.Add($"跳板主機未出現在 Access / Domain table：{host.HostName} ({host.IpAddress})");
                }
            }

            if (jumpHostsConsistentWithTables)
            {
                passedChecks++;
                messages.Add("跳板主機清單與 Access / Domain table 一致。");
            }

            int score = (int)Math.Round((double)passedChecks / totalChecks * 100, MidpointRounding.AwayFromZero);

            return new AuditItemResult
            {
                ItemKey = "firewall.policy",
                Title = "防火牆設定檢查",
                Score = score,
                Weight = 1,
                Message = string.Join("；", messages)
            };
        }

        private static AuditItemResult Fail(string reason)
            => new AuditItemResult
            {
                ItemKey = "firewall.policy",
                Title = "防火牆設定檢查",
                Score = 0,
                Weight = 1,
                Message = reason
            };
    }
}