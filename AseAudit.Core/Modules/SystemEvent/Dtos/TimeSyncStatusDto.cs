using System;

namespace AseAudit.Core.Modules.SystemEvent.Dtos
{
    public sealed class TimeSyncStatusDto
    {
        // SR2.11 時戳 / 時間同步
        public bool FeatureAvailable { get; set; }
        public bool WindowsScheduleConfigured { get; set; }
        public bool OaTimeSyncEnabled { get; set; }
        public string? TimeSource { get; set; }
        public DateTime? LastSyncAt { get; set; }
    }
}
