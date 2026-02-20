using System;
using System.Collections.Generic;
using System.Linq;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules;

public sealed class ErrorFeedbackRule
{
    public AuditItemResult Evaluate(UiControlSnapshotDto s)
    {
        var text = (s.OcrText ?? string.Empty).Trim();

        // 是否有任何「錯誤回饋」跡象（你可依系統調整關鍵字）
        var hasErrorFeedback = ContainsAny(text,
            "錯誤", "失敗", "error", "failed", "無法", "拒絕", "invalid", "不正確", "incorrect", "登入失敗", "login failed");

        if (!hasErrorFeedback)
        {
            return new AuditItemResult
            {
                ItemKey = "SR1.10",
                Score = 0,
                Passed = false,
                Title = "錯誤回饋",
                Message = "未觀察到登入/操作失敗的錯誤回饋提示。",
                Detail = new Dictionary<string, object?>
                {
                    ["ScreenName"] = s.ScreenName,
                    ["HasErrorFeedback"] = false
                }
            };
        }

        // 是否洩漏「帳號是否存在」
        var leaksAccountExistence = ContainsAny(text,
            "帳號不存在", "查無此帳號", "使用者不存在", "user not found", "no such user", "account not found", "unknown user");

        if (leaksAccountExistence)
        {
            return new AuditItemResult
            {
                ItemKey = "SR1.10",
                Score = 50,
                Passed = false,
                Title = "錯誤回饋",
                Message = "有錯誤回饋，但可能洩漏「帳號是否存在」（建議統一回覆為帳號或密碼錯誤）。",
                Detail = new Dictionary<string, object?>
                {
                    ["ScreenName"] = s.ScreenName,
                    ["HasErrorFeedback"] = true,
                    ["LeaksAccountExistence"] = true
                }
            };
        }

        return new AuditItemResult
        {
            ItemKey = "SR1.10",
            Score = 100,
            Passed = true,
            Title = "錯誤回饋",
            Message = "有錯誤回饋且未觀察到洩漏「帳號是否存在」的提示（較符合安全作法）。",
            Detail = new Dictionary<string, object?>
            {
                ["ScreenName"] = s.ScreenName,
                ["HasErrorFeedback"] = true,
                ["LeaksAccountExistence"] = false
            }
        };
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        var t = (text ?? string.Empty).ToLowerInvariant();
        return keywords.Any(k => t.Contains(k.ToLowerInvariant()));
    }
}
