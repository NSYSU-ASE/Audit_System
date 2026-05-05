using AseAudit.Api.Services.Ingest.Firewall;
using AseAudit.Api.Services.Ingest.Identity;

namespace AseAudit.Api.Services.Ingest;

/// <summary>
/// 集中註冊所有 <see cref="ISnapshotHandler"/> 實作。
/// 新增 ScriptName 時，只需在此類別加入一行 AddScoped，不需修改 Program.cs。
/// </summary>
public static class SnapshotHandlerRegistration
{
    public static IServiceCollection AddSnapshotHandlers(this IServiceCollection services)
    {
        // Identity 模組
        services.AddScoped<ISnapshotHandler, HostAccountSnapshotHandler>();
        services.AddScoped<ISnapshotHandler, HostAccountRuleSnapshotHandler>();

        // Firewall 模組
        services.AddScoped<ISnapshotHandler, FirewallRuleSnapshotHandler>();

        return services;
    }
}
