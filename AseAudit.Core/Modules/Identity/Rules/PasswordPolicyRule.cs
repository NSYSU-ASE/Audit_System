using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules;

public sealed class PasswordPolicyRule
{
    public AuditItemResult Evaluate(PasswordPolicySnapshotDto s)
    {
        const string title = "公司密碼原則標準";

        // 三個關鍵條件（對應你的投影片三個 +30）
        var hasComplexity = s.PasswordComplexityEnabled;
        var hasLockout = s.LockoutThreshold > 0;
        var has90DaysRotation = s.MaxPasswordAgeDays > 0 && s.MaxPasswordAgeDays <= 90;

        // 計分：基礎 10 + 每個命中 +30，封頂 100
        var score = 10;
        if (hasComplexity) score += 30;
        if (hasLockout) score += 30;
        if (has90DaysRotation) score += 30;
        if (score > 100) score = 100;

        var passed = (score >= 70); // 你可自行調整門檻

        // 產生訊息（給前端/Swagger 看）
        var reasons = new List<string>();
        reasons.Add(hasComplexity ? "✅ 已啟用密碼複雜度" : "❌ 未啟用密碼複雜度");
        reasons.Add(hasLockout ? $"✅ 已設定登入嘗試限制（門檻={s.LockoutThreshold}）" : "❌ 未設定登入嘗試限制（LockoutThreshold=0）");
        reasons.Add(has90DaysRotation ? $"✅ 已設定密碼最長使用期限（{s.MaxPasswordAgeDays} 天）" : $"❌ 未符合 90 天更換（目前 {s.MaxPasswordAgeDays} 天）");

        if (s.SameAsDomainPolicy.HasValue)
            reasons.Add(s.SameAsDomainPolicy.Value ? "✅ 與 AD Domain 原則一致" : "⚠️ 與 AD Domain 原則不一致");

        var message = string.Join("；", reasons);

        return new AuditItemResult
        {
            ItemKey = "SR1.7-password-policy",
            Score = score,
            Weight = 1.0,
            Passed = passed,
            // 下面兩個欄位「看你 AuditItemResult 有沒有」
            Title = title,
            Message = message,
            Detail = new Dictionary<string, object?>
            {
                ["MinPasswordLength"] = s.MinPasswordLength,
                ["MaxPasswordAgeDays"] = s.MaxPasswordAgeDays,
                ["LockoutThreshold"] = s.LockoutThreshold,
                ["PasswordHistoryLength"] = s.PasswordHistoryLength,
                ["ComplexityEnabled"] = s.PasswordComplexityEnabled,
                ["SameAsDomainPolicy"] = s.SameAsDomainPolicy
            }
        };
    }
}
