namespace ASEAudit.Shared.Contracts;

/// <summary>
/// [FirewallRuleSnapshot] Collector → API 的 Payload 格式。
/// 對應 PowerShell 腳本 <c>FirewallRuleSnapshot.Content</c> 的 JSON 輸出，
/// 每筆 <see cref="FirewallRuleEntry"/> 展開後寫入資料表 <c>[dbo].[FireWallRule]</c>。
///
/// 欄位對應 (Payload → Entity <c>FireWallRule</c>)：
///   Hostname                    → HostName
///   Payload.Rules[*].Name       → RuleName
///   Payload.Rules[*].DisplayName→ DisplayName
///   Payload.Rules[*].Enabled    → Status
///   Payload.Rules[*].Profile    → Profile
///   Payload.Rules[*].Direction  → Direction
///   Payload.Rules[*].Action     → Action
///   Payload.Rules[*].Protocol   → Protocol
///   Payload.Rules[*].LocalPort  → Port (LocalPort 欄位)
///   Payload.Rules[*].RemotePort → RemotePort
///   Payload.Rules[*].LocalAddress  → SourceIP
///   Payload.Rules[*].RemoteAddress → DestinationIP
///   (MACAddress 由 Server 端補齊)
/// </summary>
public sealed class FirewallRuleSnapshotPayload : ScriptPayload<FirewallRuleSnapshotContent>
{
    /// <summary>Agent / Server 共用的腳本名稱常數 (單一真相來源)。</summary>
    public const string Script = "FirewallRuleSnapshot";

    /// <summary>此 Payload 對應的資料表名稱，供 Ingest 層路由使用。</summary>
    public const string TableName = "FireWallRule";

    /// <inheritdoc />
    public override string ScriptName { get; set; } = Script;
}

/// <summary>
/// <see cref="FirewallRuleSnapshotPayload"/> 的實際稽核資料內容。
/// 對應 PowerShell 輸出 JSON 中 <c>Payload</c> 物件。
/// </summary>
public sealed class FirewallRuleSnapshotContent : PayloadWrapper
{
    /// <summary>
    /// 防火牆規則清單 (Get-NetFirewallRule，含對應的 PortFilter / AddressFilter)。
    /// 每筆展開為 FireWallRule 一列。
    /// </summary>
    public List<FirewallRuleEntry> Rules { get; init; } = [];
}

/// <summary>
/// 單一防火牆規則資訊。每個實例對應資料表 <c>FireWallRule</c> 一列。
/// 多值欄位 (LocalPort / RemotePort / LocalAddress / RemoteAddress) 在 PowerShell 端
/// 已 join 為以逗號分隔的字串。
/// </summary>
public sealed class FirewallRuleEntry
{
    /// <summary>規則名稱 (Get-NetFirewallRule.Name)，寫入 RuleName 欄位。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>顯示名稱 (Get-NetFirewallRule.DisplayName)，寫入 DisplayName 欄位。</summary>
    public string? DisplayName { get; init; }

    /// <summary>啟用狀態 ("True" / "False")，寫入 Status 欄位。</summary>
    public string? Enabled { get; init; }

    /// <summary>套用設定檔 (Domain / Private / Public / Any)，寫入 Profile 欄位。</summary>
    public string? Profile { get; init; }

    /// <summary>規則方向 (Inbound / Outbound)，寫入 Direction 欄位。</summary>
    public string? Direction { get; init; }

    /// <summary>規則動作 (Allow / Block)，寫入 Action 欄位。</summary>
    public string? Action { get; init; }

    /// <summary>協定 (TCP / UDP / ICMPv4 ...)，寫入 Protocol 欄位。</summary>
    public string? Protocol { get; init; }

    /// <summary>本機通訊埠，寫入 LocalPort 欄位（多值以逗號分隔）。</summary>
    public string? LocalPort { get; init; }

    /// <summary>遠端通訊埠，寫入 RemotePort 欄位（多值以逗號分隔）。</summary>
    public string? RemotePort { get; init; }

    /// <summary>本機位址，寫入 SourceIP 欄位（多值以逗號分隔）。</summary>
    public string? LocalAddress { get; init; }

    /// <summary>遠端位址，寫入 DestinationIP 欄位（多值以逗號分隔）。</summary>
    public string? RemoteAddress { get; init; }
}
