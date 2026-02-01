using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules;

/// <summary>
/// 公司密碼原則標準（流程圖：windows AD / 帳號是否 AD / 是否 administrator）
/// 分數：100 / 80 / 40 / 0
/// </summary>
public sealed class AdAccountProtectionRule
{
    public AuditItemResult Evaluate(HostAccountSnapshotDto s)
    {
        const string itemKey = "M1_PASSWORD_POLICY_AD_PROTECTION";

        // 1) 本機有無 AD？
        if (s.HasAd)
        {
            // 2) 登入是否為 AD 帳號？
            if (s.IsAdAccount == true)
            {
                return new AuditItemResult
                {
                    ItemKey = itemKey,
                    Score = 100,
                    Passed = true,
                    Detail = new Dictionary<string, object?>
                    {
                        ["reason"] = "Host has AD and login is AD account => fully protected.",
                        ["has_ad"] = s.HasAd,
                        ["is_ad_account"] = s.IsAdAccount,
                        ["login_account"] = s.LoginAccount
                    }
                };
            }

            // 有 AD 但登入不是 AD 帳號（或未知）→ 80
            return new AuditItemResult
            {
                ItemKey = itemKey,
                Score = 80,
                Passed = false,
                Detail = new Dictionary<string, object?>
                {
                    ["reason"] = "Host has AD but login is NOT AD account (or unknown) => partial.",
                    ["has_ad"] = s.HasAd,
                    ["is_ad_account"] = s.IsAdAccount,
                    ["login_account"] = s.LoginAccount
                }
            };
        }

        // 3) 無 AD：看是否為 administrator（越級）
        if (s.IsLocalAdmin)
        {
            return new AuditItemResult
            {
                ItemKey = itemKey,
                Score = 0,
                Passed = false,
                Detail = new Dictionary<string, object?>
                {
                    ["reason"] = "No AD + local admin => highest risk.",
                    ["has_ad"] = s.HasAd,
                    ["is_local_admin"] = s.IsLocalAdmin,
                    ["login_account"] = s.LoginAccount
                }
            };
        }

        // 無 AD + 不是 admin → 40
        return new AuditItemResult
        {
            ItemKey = itemKey,
            Score = 40,
            Passed = false,
            Detail = new Dictionary<string, object?>
            {
                ["reason"] = "No AD but not local admin => partial score (still unmanaged).",
                ["has_ad"] = s.HasAd,
                ["is_local_admin"] = s.IsLocalAdmin,
                ["login_account"] = s.LoginAccount
            }
        };
    }
}

