using Microsoft.Extensions.DependencyInjection;

namespace AseAudit.Infrastructure.Repositories;

/// <summary>
/// 集中註冊所有 Repository 實作。
/// 新增資料表 Repository 時，只需在此類別加入一行 AddScoped，不需修改 Program.cs。
/// 生命週期須與 <c>AuditDbContext</c> 一致（Scoped），避免跨 request 共用 DbContext。
/// </summary>
public static class RepositoryRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Identity 模組
        services.AddScoped<IIdentificationAmAccountRepository, IdentificationAmAccountRepository>();
        services.AddScoped<IIdentificationAmRuleRepository, IdentificationAmRuleRepository>();

        // Firewall 模組
        services.AddScoped<IFireWallRuleRepository, FireWallRuleRepository>();

        return services;
    }
}
