using System;
using System.Collections.Generic;
using System.Linq;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules;

public sealed class SystemUseNoticeRule
{
    public AuditItemResult Evaluate(UiControlSnapshotDto s)
    {
        var text = (s.OcrText ?? string.Empty).Trim();

        // 是否有「使用通知/警語」跡象（你可依公司 Banner 語句調整）
        var hasNotice = ContainsAny(text,
            "未經授權", "禁止", "authorized", "unauthorized",
            "本系統", "system",
            "監控", "監視", "logged", "audit", "記錄", "紀錄",
            "警告", "warning",
            "使用者同意", "consent");

        if (!hasNotice)
        {
            return new AuditItemResult
            {
                ItemKey = "SR1.12",
                Score = 50,
                Passed = false,
                Title = "系統使用通知",
                Message = "未觀察到系統使用通知/警語（建議於登入前或首頁顯示使用政策與監控告知）。",
                Detail = new Dictionary<string, object?>
                {
                    ["ScreenName"] = s.ScreenName,
                    ["HasSystemUseNotice"] = false
                }
            };
        }

        return new AuditItemResult
        {
            ItemKey = "SR1.12",
            Score = 100,
            Passed = true,
            Title = "系統使用通知",
            Message = "已觀察到系統使用通知/警語（符合使用告知/監控告知的方向）。",
            Detail = new Dictionary<string, object?>
            {
                ["ScreenName"] = s.ScreenName,
                ["HasSystemUseNotice"] = true
            }
        };
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        var t = (text ?? string.Empty).ToLowerInvariant();
        return keywords.Any(k => t.Contains(k.ToLowerInvariant()));
    }
}
