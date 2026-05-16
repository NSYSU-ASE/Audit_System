namespace AseAudit.Core.Modules.SoftwareRecognition.Dtos
{
    /// <summary>
    /// IP Guard / 端點控管狀態（資料由廠商 DB 或代理程式回報）。
    /// </summary>
    public sealed class IpGuardStatusRecordDto
    {
        public string DeviceId { get; set; } = "";
        public bool IsInstalled { get; set; }
        public bool IsConnected { get; set; }
        public string? Status { get; set; }

        // SR2.3：可攜式裝置及行動裝置使用控制。
        public bool PortableDeviceControlEnabled { get; set; }

        // SR2.4：行動程式碼執行限制。
        public bool MobileCodeControlEnabled { get; set; }

        public DateTime? LastConnectedAt { get; set; }
    }
}
