using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AseAudit.Core.Entities
{
    /// <summary>
    /// 對應資料表 [dbo].[FireWallRule] — 防火牆規則
    /// </summary>
    [Table("FireWallRule")]
    public class FireWallRule
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
        [Column("RuleName")]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        [Column("DisplayName")]
        [MaxLength(100)]
        public string? DisplayName { get; set; }

        [Column("Status")]
        [MaxLength(100)]
        public string? Status { get; set; }

        [Column("Profile")]
        [MaxLength(100)]
        public string? Profile { get; set; }

        [Column("Action")]
        [MaxLength(100)]
        public string? Action { get; set; }


        [Column("Direction")]
        [MaxLength(100)]
        public string? Direction { get; set; }

        [Column("LocalPort")]
        [MaxLength(100)]
        public string? Port { get; set; }

        [Column("RemotePort")]
        [MaxLength(100)]
        public string? RemotePort { get; set; }

        [Column("Protocol")]
        [MaxLength(100)]
        public string? Protocol { get; set; }

        [Column("SourceIP")]
        [MaxLength(100)]
        public string? SourceIP { get; set; }

        [Column("DestinationIP")]
        [MaxLength(100)]
        public string? DestinationIP { get; set; }
    }
}
