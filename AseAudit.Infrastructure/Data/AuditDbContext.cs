using AseAudit.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AseAudit.Infrastructure.Data
{
    /// <summary>
    /// EF Core 資料庫連線設定，負責管理稽核系統的資料表對應
    /// </summary>
    public class AuditDbContext : DbContext
    {
        public AuditDbContext(DbContextOptions<AuditDbContext> options)
            : base(options)
        {
        }

        /// <summary>帳號管理資料表</summary>

        public DbSet<IdentificationAmAccount> IdentificationAmAccounts { get; set; }

        /// <summary>帳號管理規則資料表（密碼政策等）</summary>
        public DbSet<IdentificationAmRule> IdentificationAmRules { get; set; }

        /// <summary>防火牆規則資料表</summary>
        public DbSet<FireWallRule> FireWallRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identification_AM_Account 設定
            modelBuilder.Entity<IdentificationAmAccount>(entity =>
            {
                entity.HasIndex(e => e.HostName)
                      .HasDatabaseName("IX_Identification_AM_Account_HostName");

                entity.Property(e => e.CreatedTime)
                      .HasDefaultValueSql("GETDATE()");
            });

            // Identification_AM_rule 設定
            modelBuilder.Entity<IdentificationAmRule>(entity =>
            {
                entity.HasIndex(e => e.HostName)
                      .HasDatabaseName("IX_Identification_AM_rule_HostName");

                entity.Property(e => e.CreatedTime)
                      .HasDefaultValueSql("GETDATE()");
            });

            // FireWallRule 設定
            modelBuilder.Entity<FireWallRule>(entity =>
            {
                entity.HasIndex(e => e.HostName)
                      .HasDatabaseName("IX_FireWallRule_HostName");

                entity.Property(e => e.CreatedTime)
                      .HasDefaultValueSql("GETDATE()");
            });
        }
    }
}
