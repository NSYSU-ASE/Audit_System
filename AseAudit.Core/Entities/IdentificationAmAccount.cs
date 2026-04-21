using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AseAudit.Core.Entities
{
    /// <summary>
    /// 對應資料表 [dbo].[Identification_AM_Account] — 帳號管理
    /// </summary>
    [Table("Identification_AM_Account")]
    public class IdentificationAmAccount
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

        [Required]
        [Column("AccountName")]
        [MaxLength(100)]
        public string AccountName { get; set; } = string.Empty;

        [Column("Status")]
        [MaxLength(100)]
        public string? Status { get; set; }

        [Column("PasswordRequired")]
        public bool? PasswordRequired { get; set; }
    }
}
