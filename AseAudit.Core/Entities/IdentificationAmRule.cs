using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AseAudit.Core.Entities
{
    /// <summary>
    /// 對應資料表 [dbo].[Identification_AM_rule] — 帳號管理規則（密碼政策等）
    /// </summary>
    [Table("Identification_AM_rule")]
    public class IdentificationAmRule
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

        [Required]
        [Column("MACAddress")]
        [MaxLength(45)]
        public string MACAddress { get; set; } = string.Empty;

        [Required]
        [Column("UserDomain")]
        [MaxLength(100)]
        public string UserDomain { get; set; } = string.Empty;

        [Column("MinPasswordLength")]
        public int? MinPasswordLength { get; set; }

        [Column("PasswordComplexity")]
        public int? PasswordComplexity { get; set; }

        [Column("Passwordattempts")]
        public int? Passwordattempts { get; set; }

        [Column("accountlockoutduration")]
        public int? AccountLockoutDuration { get; set; }
    }
}
