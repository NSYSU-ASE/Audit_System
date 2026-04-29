using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AseAudit.Core.Entities
{
    /// <summary>
    /// 對應資料表 [dbo].[Identification_PasswordPolicy] — 本機密碼原則 / 鎖定原則 / 內建帳號設定
    /// 來源：secedit /export /areas SECURITYPOLICY 之 INF [System Access] 區段。
    /// </summary>
    [Table("Identification_PasswordPolicy")]
    public class IdentificationPasswordPolicy
    {
        [Key]
        [Column("ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; }

        [Required]
        [Column("HostName")]
        [MaxLength(255)]
        public string HostName { get; set; } = string.Empty;

        [Column("MACAddress")]
        [MaxLength(45)]
        public string? MACAddress { get; set; }

        // ── 密碼原則 ────────────────────────────────────────────

        /// <summary>密碼長度下限（字元數）。</summary>
        [Column("MinimumPasswordLength")]
        public int? MinimumPasswordLength { get; set; }

        /// <summary>密碼最長使用期限（天）；0 = 永不過期。</summary>
        [Column("MaximumPasswordAge")]
        public int? MaximumPasswordAge { get; set; }

        /// <summary>密碼最短使用期限（天）。</summary>
        [Column("MinimumPasswordAge")]
        public int? MinimumPasswordAge { get; set; }

        /// <summary>密碼歷程記錄保留筆數。</summary>
        [Column("PasswordHistorySize")]
        public int? PasswordHistorySize { get; set; }

        /// <summary>密碼複雜度需求；1 = 啟用，0 = 停用。</summary>
        [Column("PasswordComplexity")]
        public int? PasswordComplexity { get; set; }

        // ── 帳號鎖定原則 ────────────────────────────────────────

        /// <summary>帳號鎖定閾值（連續失敗登入次數）；0 = 不鎖定。</summary>
        [Column("LockoutBadCount")]
        public int? LockoutBadCount { get; set; }

        /// <summary>鎖定持續時間（分鐘）；-1 = 直到管理員手動解除。</summary>
        [Column("LockoutDuration")]
        public int? LockoutDuration { get; set; }

        /// <summary>失敗計數重設視窗（分鐘）。</summary>
        [Column("ResetLockoutCount")]
        public int? ResetLockoutCount { get; set; }

        // ── 內建帳號控制 ────────────────────────────────────────

        /// <summary>是否啟用內建 Administrator 帳號；1 = 啟用，0 = 停用。</summary>
        [Column("EnableAdminAccount")]
        public int? EnableAdminAccount { get; set; }

        /// <summary>是否啟用內建 Guest 帳號；1 = 啟用，0 = 停用。</summary>
        [Column("EnableGuestAccount")]
        public int? EnableGuestAccount { get; set; }

        /// <summary>內建 Administrator 帳號重新命名後的名稱。</summary>
        [Column("NewAdministratorName")]
        [MaxLength(100)]
        public string? NewAdministratorName { get; set; }

        /// <summary>內建 Guest 帳號重新命名後的名稱。</summary>
        [Column("NewGuestName")]
        [MaxLength(100)]
        public string? NewGuestName { get; set; }
    }
}
