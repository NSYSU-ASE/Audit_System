using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.ResourceManagement.Dtos;
using AseAudit.Core.Modules.ResourceManagement.Rules;

namespace AseAudit.Core.Modules.ResourceManagement.Services
{
    public sealed class ResourceManagementAuditService
    {
        private readonly ResourceUsageMonitoringRule _resourceUsageRule = new();
        private readonly EmergencyPowerProtectionRule _emergencyPowerRule = new();
        private readonly NetworkSecurityBaselineRule _networkSecurityRule = new();
        private readonly ComponentInventoryRule _componentInventoryRule = new();

        public ResourceManagementAuditSummaryDto Evaluate(ResourceManagementSnapshotDto snapshot)
        {
            var results = new List<AuditItemResult>
            {
                _resourceUsageRule.Evaluate(snapshot.Monitoring),
                _emergencyPowerRule.Evaluate(snapshot.EmergencyPower),
                _networkSecurityRule.Evaluate(snapshot.TopologyAssets, snapshot.SecurityBaselines),
                _componentInventoryRule.Evaluate(snapshot.TopologyAssets, snapshot.ComponentInventory)
            };

            int totalScore = results.Count == 0
                ? 0
                : (int)results.Average(x => x.Score);

            return new ResourceManagementAuditSummaryDto
            {
                DeviceId = snapshot.DeviceId,
                Results = results,
                TotalScore = totalScore
            };
        }
    }
}