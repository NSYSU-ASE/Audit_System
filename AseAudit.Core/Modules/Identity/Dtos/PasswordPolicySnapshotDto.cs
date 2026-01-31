using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Identity.Dtos;

public sealed class PasswordPolicySnapshotDto
{
    // 你之後可以把 RawNetAccountsText 留著，方便 Debug/追溯
    public string? RawNetAccountsText { get; init; }

    // net accounts 可取得
    public int MinPasswordLength { get; init; }              // 密碼長度下限
    public int MaxPasswordAgeDays { get; init; }             // 最長使用期限(天)，例：90
    public int MinPasswordAgeDays { get; init; }             // 最短使用期限(天)
    public int PasswordHistoryLength { get; init; }          // 密碼歷程記錄數
    public int LockoutThreshold { get; init; }               // 鎖定門檻(次)
    public int LockoutDurationMinutes { get; init; }         // 鎖定持續時間(分)
    public int LockoutObservationWindowMinutes { get; init; }// 觀測視窗(分)

    // 這個通常不是 net accounts 來的，但先假設你拿得到
    public bool PasswordComplexityEnabled { get; init; }     // 密碼複雜度是否啟用

    // 你想比對「是否跟 AD Domain 一樣」的話，先留欄位（之後接真資料再用）
    public bool? SameAsDomainPolicy { get; init; }           // optional
}

