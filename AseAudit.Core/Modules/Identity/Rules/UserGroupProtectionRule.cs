using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules;

public sealed class UserGroupRule
{
    public AuditItemResult Evaluate(UserGroupSnapshotDto s)
    {
        // 0) 基本防呆
        if (s is null)
        {
            return new AuditItemResult
            {
                ItemKey = "identity.user_group",
                Score = 0,
                Weight = 1,
                Passed = false,
                Title = "使用者分群",
                Message = "未取得任何分群資料，無法判斷。",
            };
        }

        // 1) 沒有群組設定 -> 0 分
        if (!s.HasGroupConfig)
        {
            return new AuditItemResult
            {
                ItemKey = "identity.user_group",
                Score = 0,
                Weight = 1,
                Passed = false,
                Title = "使用者分群",
                Message = "未設定群組清單/分群規則（RPT 群組清單不存在），不符合最小權限原則。",
                Detail = new()
                {
                    ["UserAccount"] = s.UserAccount,
                    ["HasGroupConfig"] = s.HasGroupConfig
                }
            };
        }

        // 2) 有群組設定：檢查是否在應屬群組
        var inExpected = s.ActualGroups != null
            && s.ActualGroups.Any(g => string.Equals(g, s.ExpectedGroup, StringComparison.OrdinalIgnoreCase));

        if (inExpected)
        {
            return new AuditItemResult
            {
                ItemKey = "identity.user_group",
                Score = 100,
                Weight = 1,
                Passed = true,
                Title = "使用者分群",
                Message = "使用者在對應群組中，符合分群控管要求。",
                Detail = new()
                {
                    ["UserAccount"] = s.UserAccount,
                    ["ExpectedGroup"] = s.ExpectedGroup,
                    ["ActualGroups"] = s.ActualGroups
                }
            };
        }

        // 3) 有群組設定但不在 -> 50 分
        return new AuditItemResult
        {
            ItemKey = "identity.user_group",
            Score = 50,
            Weight = 1,
            Passed = false,
            Title = "使用者分群",
            Message = "使用者不在對應群組中，可能存在帳號權限與實際職責不一致、異動後未更新等風險。",
            Detail = new()
            {
                ["UserAccount"] = s.UserAccount,
                ["ExpectedGroup"] = s.ExpectedGroup,
                ["ActualGroups"] = s.ActualGroups
            }
        };
    }
}
