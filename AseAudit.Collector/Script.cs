using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;
using AseAudit.Collector.Script_lib;

namespace AseAudit.Collector;

// ─────────────────────────────────────────────
//  回傳型別：腳本執行結果包裝
// ─────────────────────────────────────────────

/// <summary>
/// 腳本執行結果包裝，供其他 .cs 呼叫時取得輸出與狀態。
/// </summary>
public sealed class ScriptResult
{
    public bool Success { get; init; }
    public string RawOutput { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

    /// <summary>將 RawOutput 反序列化為指定型別（腳本應輸出 JSON）。</summary>
    public T? Deserialize<T>() =>
        string.IsNullOrWhiteSpace(RawOutput)
            ? default
            : JsonSerializer.Deserialize<T>(RawOutput, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
}

// ─────────────────────────────────────────────
//  介面：讓其他 .cs 依賴注入 / 測試時 mock
// ─────────────────────────────────────────────

public interface IScriptExecutor
{
    Task<ScriptResult> RunAsync(string script, CancellationToken ct = default);
}

// ─────────────────────────────────────────────
//  實作：PowerShell 執行引擎
// ─────────────────────────────────────────────

/// <summary>
/// 使用 PowerShell SDK 在 In-Process Runspace 執行腳本。
/// 需要 NuGet：System.Management.Automation
/// </summary>
public sealed class PowerShellExecutor : IScriptExecutor, IDisposable
{
    private readonly ILogger<PowerShellExecutor> _logger;
    private readonly RunspacePool _pool;

    public PowerShellExecutor(ILogger<PowerShellExecutor> logger)
    {
        _logger = logger;

        var iss = InitialSessionState.CreateDefault();
        iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;
        _pool = RunspaceFactory.CreateRunspacePool(1, Environment.ProcessorCount, iss, null);
        _pool.Open();
    }

    public async Task<ScriptResult> RunAsync(string script, CancellationToken ct = default)
    {
        using var ps = PowerShell.Create();
        ps.RunspacePool = _pool;
        ps.AddScript(script);

        try
        {
            var results = await Task.Run(() => ps.Invoke(), ct);
            var output = string.Join(Environment.NewLine, results.Select(r => r?.ToString() ?? string.Empty));

            if (ps.HadErrors)
            {
                var err = string.Join("; ", ps.Streams.Error.Select(e => e.ToString()));
                _logger.LogWarning("PowerShell errors: {Errors}", err);
            }

            return new ScriptResult { Success = !ps.HadErrors, RawOutput = output };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PowerShell execution failed");
            return new ScriptResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public void Dispose() => _pool.Dispose();
}

// ─────────────────────────────────────────────
//  收集服務：組合執行器 + 腳本，供 Worker / API 呼叫
//  腳本內容已移至 Script_lib/ 資料夾，透過 ScriptRegistry 管理
// ─────────────────────────────────────────────

/// <summary>
/// 對外提供各模組資料收集方法，注入 IScriptExecutor 即可替換執行策略。
/// </summary>
public sealed class CollectionService
{
    private readonly IScriptExecutor _executor;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(IScriptExecutor executor, ILogger<CollectionService> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    // ── Identity ─────────────────────────────

    public Task<ScriptResult> CollectHostAccountAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(HostAccountSnapshot), HostAccountSnapshot.Content, ct);

    public Task<ScriptResult> CollectPasswordPolicyAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(PasswordPolicySnapshot), PasswordPolicySnapshot.Content, ct);

    public Task<ScriptResult> CollectUserGroupsAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(UserGroupSnapshot), UserGroupSnapshot.Content, ct);

    // ── SoftwareControl ───────────────────────

    public Task<ScriptResult> CollectInstalledProgramsAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(InstalledProgramsSnapshot), InstalledProgramsSnapshot.Content, ct);

    public Task<ScriptResult> CollectAntivirusStatusAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(AntivirusStatusSnapshot), AntivirusStatusSnapshot.Content, ct);

    // ── Firewall ──────────────────────────────

    public Task<ScriptResult> CollectFirewallPolicyAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(FirewallPolicySnapshot), FirewallPolicySnapshot.Content, ct);

    public Task<ScriptResult> CollectNetworkInterfacesAsync(CancellationToken ct = default)
        => RunAndLogAsync(nameof(NetworkInterfaceSnapshot), NetworkInterfaceSnapshot.Content, ct);

    // ── 私有輔助 ──────────────────────────────

    private async Task<ScriptResult> RunAndLogAsync(string name, string script, CancellationToken ct)
    {
        _logger.LogInformation("Collecting [{Script}]...", name);
        var result = await _executor.RunAsync(script, ct);

        if (result.Success)
            _logger.LogInformation("[{Script}] OK ({Length} chars)", name, result.RawOutput.Length);
        else
            _logger.LogWarning("[{Script}] FAILED: {Error}", name, result.ErrorMessage);

        return result;
    }
}
