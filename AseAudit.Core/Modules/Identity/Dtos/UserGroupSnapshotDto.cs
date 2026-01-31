using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Identity.Dtos;

public sealed class UserGroupSnapshotDto
{
    public bool HasGroupConfig { get; init; }          // 是否有群組設定
    public string UserAccount { get; init; } = "";     // 帳號
    public string ExpectedGroup { get; init; } = "";   // 應該在的群組
    public List<string> ActualGroups { get; init; } = new();
}