using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASEAudit.Shared.Scoring;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Core.Modules.Identity.Rules
{
    /// <summary>
    /// 檢查登入的 AD 帳號是否存在於員工資料庫（在職=100，離職/不存在=0）
    /// </summary>
    public sealed class EmployeeDirectoryProtectionRule
    {
        /// <param name="host">目前登入資訊</param>
        /// <param name="employees">員工資料庫（由 DB 撈出來的清單）</param>
        public AuditItemResult Evaluate(
            HostIdentitySnapshotDto host,
            IEnumerable<EmployeeDirectoryRecordDto> employees)
        {
            var account = (host.LoggedInAdAccount ?? "").Trim();

            // 沒有登入帳號 → 視為不合格（0）
            if (string.IsNullOrWhiteSpace(account))
            {
                return new AuditItemResult
                {
                    ItemKey = "employee_directory_check",
                    Title = "員工資料庫帳號驗證",
                    Score = 0,
                    Weight = 1,
                    Message = "未取得登入 AD 帳號，無法驗證是否為在職員工。"
                };
            }

            // 統一比對格式（去掉可能的網域前綴 ASE\xxx）
            account = NormalizeAdAccount(account);

            // 在員工資料庫中尋找
            var match = employees.FirstOrDefault(e =>
                string.Equals(NormalizeAdAccount(e.AdAccount), account, StringComparison.OrdinalIgnoreCase));

            // 找不到 or 離職 → 0
            if (match is null || match.IsActive == false)
            {
                return new AuditItemResult
                {
                    ItemKey = "employee_directory_check",
                    Title = "員工資料庫帳號驗證",
                    Score = 0,
                    Weight = 1,
                    Message = "登入帳號未在員工資料庫中（或已離職），存在帳號被濫用風險。"
                };
            }

            // 在職 → 100
            return new AuditItemResult
            {
                ItemKey = "employee_directory_check",
                Title = "員工資料庫帳號驗證",
                Score = 100,
                Weight = 1,
                Message = "登入帳號存在於員工資料庫（在職），符合存取控管要求。"
            };
        }

        private static string NormalizeAdAccount(string input)
        {
            var s = input.Trim();

            // 支援格式：ASE\james.chen → james.chen
            var slashIndex = s.IndexOf('\\');
            if (slashIndex >= 0 && slashIndex + 1 < s.Length)
                s = s[(slashIndex + 1)..];

            // 支援 UPN：james.chen@ase.com → james.chen
            var atIndex = s.IndexOf('@');
            if (atIndex > 0)
                s = s[..atIndex];

            return s.Trim();
        }
    }
}

