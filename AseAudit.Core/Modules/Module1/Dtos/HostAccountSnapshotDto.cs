using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Module1.Dtos;

public sealed class HostAccountSnapshotDto
{
    public string HostId { get; init; } = string.Empty;
    public string Hostname { get; init; } = string.Empty;

    // 流程圖：本機有無 AD
    public bool HasAd { get; init; }

    // 流程圖：登入者是否為 AD 帳號（未知可為 null，當作不是）
    public bool? IsAdAccount { get; init; }

    // 流程圖：是否具有 administrator 權限（越級）
    public bool IsLocalAdmin { get; init; }

    // 可選：留著當證據顯示
    public string? LoginAccount { get; init; }
}
