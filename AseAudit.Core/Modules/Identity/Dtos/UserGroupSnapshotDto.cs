using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AseAudit.Core.Modules.Identity.Dtos;

public sealed class UserGroupSnapshotDto
{
    /// <summary>是否有群組設定（是否存在 RPT 群組清單/規則）</summary>
    public bool HasGroupConfig { get; init; }

    /// <summary>要檢查的帳號</summary>
    public string UserAccount { get; init; } = "";

    /// <summary>帳號應該屬於的群組（例如：RPT_Operator / RPT_Admin）</summary>
    public string ExpectedGroup { get; init; } = "";

    /// <summary>帳號實際所在的群組清單（從系統/AD/RPT 匯出來的結果）</summary>
    public List<string> ActualGroups { get; init; } = new();
}
