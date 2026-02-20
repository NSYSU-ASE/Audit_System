using System;
using System.Collections.Generic;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules;

/// <summary>
/// 帳號是否受 AD 保護（流程：本機有無 AD → 登入是否 AD 帳號 → 是否 Local Admin 越權）
/// 分數：100 / 80 / 40 / 0
/// </summary>
public sealed class AdAccountProtectionRule
{
    private const string ItemKeyConst = "IDENTITY_AD_ACCOUNT_PROTECTION";

    public AuditItemResult Evaluate(HostAccountSnapshotDto s)
    {
        // 依「走到哪個分支」決定風險與分數（風險不是寫在資料裡，是程式判斷出來的）
        if (s.HasAd)
        {
            if (s.IsAdAccount == true)
            {
                // 分支 A：有 AD + 使用 AD 帳號
                return BuildResult(
                    s,
                    score: 100,
                    passed: true,
                    branch: "HAS_AD_AND_LOGIN_IS_AD",
                    title: "AD 帳號保護",
                    message: "本機有 AD 且登入為 AD 帳號，帳號受網域控管。",
                    risks: new List<string>
                    {
                        // 100 分仍可保留「低風險提醒」，但不列為缺失
                        "仍需確認該帳號套用正確的 GPO（密碼、鎖定、稽核、最小權限）。"
                    },
                    recommendations: new List<string>
                    {
                        "確認此帳號屬於正確群組（依角色最小權限）。",
                        "確認網域 GPO（密碼複雜度/鎖定/稽核）已套用到此主機。"
                    }
                );
            }

            // 分支 B：有 AD 但登入不是 AD 帳號（或未知）
            return BuildResult(
                s,
                score: 80,
                passed: false,
                branch: "HAS_AD_BUT_LOGIN_NOT_AD_OR_UNKNOWN",
                title: "AD 帳號保護",
                message: "本機有 AD，但目前登入帳號不是 AD 帳號（或無法判定），存在繞過網域控管風險。",
                risks: new List<string>
                {
                    "使用本機帳號可能繞過網域的密碼政策/鎖定策略/稽核策略（GPO）。",
                    "離職或角色異動後，本機帳號仍可能持續可用，造成帳號生命週期失控。",
                    "本機帳號權限與網域群組控管可能不一致，增加越權或濫用風險。"
                },
                recommendations: new List<string>
                {
                    "改用 AD 帳號登入或停用不必要的本機帳號。",
                    "盤點本機帳號與群組成員（Administrators/Remote Desktop Users 等）。",
                    "若必須保留本機帳號，至少納入集中式政策與稽核（LAPS/Local user policy/EDR）。"
                }
            );
        }

        // 沒有 AD：再看是否 local admin（越權）
        if (s.IsLocalAdmin)
        {
            // 分支 C：無 AD + local admin（最高風險）
            return BuildResult(
                s,
                score: 0,
                passed: false,
                branch: "NO_AD_AND_LOCAL_ADMIN",
                title: "AD 帳號保護",
                message: "本機未加入 AD，且登入帳號具備本機系統管理權限（越權風險高）。",
                risks: new List<string>
                {
                    "帳號不受網域控管且具系統管理權限，可能修改安全設定、停用防護、植入後門。",
                    "缺乏集中式稽核與一致政策，事後追查與責任歸屬困難。",
                    "若密碼弱/共用帳號，容易被暴力破解或橫向移動擴散。"
                },
                recommendations: new List<string>
                {
                    "將主機納入 AD/集中式身份管理（如可行）。",
                    "移除不必要的本機 Admin 權限，改用分權/跳板/Just Enough Admin。",
                    "建立本機帳號政策與稽核（強密碼、鎖定、定期輪替、最小權限）。"
                }
            );
        }

        // 分支 D：無 AD + 非 admin（仍有管理分散風險）
        return BuildResult(
            s,
            score: 40,
            passed: false,
            branch: "NO_AD_AND_NOT_LOCAL_ADMIN",
            title: "AD 帳號保護",
            message: "本機未加入 AD，登入帳號非系統管理員，但帳號/政策仍屬分散管理。",
            risks: new List<string>
            {
                "帳號不受網域集中控管，密碼政策/鎖定/稽核可能與公司標準不一致。",
                "帳號異動/離職處理容易遺漏，造成帳號殘留風險。",
                "使用者可能為了方便提高權限或共用帳號，衍生後續風險。"
            },
            recommendations: new List<string>
            {
                "評估將主機納入 AD 或等效的集中式身份管理。",
                "建立本機帳號治理流程（建立/停用/權限審核/定期盤點）。",
                "至少落實密碼政策、登入鎖定、與操作稽核。"
            }
        );
    }

    private static AuditItemResult BuildResult(
        HostAccountSnapshotDto s,
        double score,
        bool passed,
        string branch,
        string title,
        string message,
        List<string> risks,
        List<string> recommendations)
    {
        return new AuditItemResult
        {
            ItemKey = ItemKeyConst,
            Score = score,
            Passed = passed,

            // 你 Swagger 有顯示 title/message，這裡一起補齊
            Title = title,
            Message = message,

            Detail = new Dictionary<string, object?>
            {
                // 用來展示「走到哪個分支」→ 風險怎麼被判斷出來
                ["branch"] = branch,

                // 證據（輸入快照）
                ["evidence"] = new Dictionary<string, object?>
                {
                    ["has_ad"] = s.HasAd,
                    ["is_ad_account"] = s.IsAdAccount,
                    ["is_local_admin"] = s.IsLocalAdmin,
                    ["login_account"] = s.LoginAccount
                },

                // 由分支推導出的風險與建議
                ["risks"] = risks,
                ["recommendations"] = recommendations
            }
        };
    }
}
