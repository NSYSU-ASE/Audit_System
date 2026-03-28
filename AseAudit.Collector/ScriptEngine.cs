using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;
using AseAudit.Collector.Script_lib;

namespace AseAudit.Collector;

// ─────────────────────────────────────────────
//  回傳型別：腳本執行結果包裝
// ─────────────────────────────────────────────

public sealed class ScriptResult
{
    public bool Success { get; init; }
    public string RawOutput { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }

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
            var output = string.Join(Environment.NewLine,
                results.Select(r => r?.ToString() ?? string.Empty));

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
//  ScriptEngine — 自動執行 Script_lib 內所有腳本
//
//  透過 ScriptRegistry.All 取得全部腳本，逐一執行並
//  回傳 Dictionary<string, ScriptResult>。
// ─────────────────────────────────────────────

public sealed class ScriptEngine
{
    private readonly IScriptExecutor _executor;
    private readonly ILogger<ScriptEngine> _logger;

    public ScriptEngine(IScriptExecutor executor, ILogger<ScriptEngine> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    /// <summary>
    /// 依序執行 ScriptRegistry.All 中註冊的所有腳本，
    /// 回傳以腳本名稱為 key、執行結果為 value 的字典。
    /// </summary>
    public async Task<Dictionary<string, ScriptResult>> RunAllAsync(CancellationToken ct = default)
    {
        var results = new Dictionary<string, ScriptResult>();

        _logger.LogInformation("ScriptEngine: 開始執行全部 {Count} 支腳本", ScriptRegistry.All.Count);

        foreach (var (name, content) in ScriptRegistry.All)
        {
            if (ct.IsCancellationRequested) break;

            _logger.LogInformation("Collecting [{Script}]...", name);
            var result = await _executor.RunAsync(content, ct);

            if (result.Success)
                _logger.LogInformation("[{Script}] OK ({Length} chars)", name, result.RawOutput.Length);
            else
                _logger.LogWarning("[{Script}] FAILED: {Error}", name, result.ErrorMessage);

            results[name] = result;
        }

        _logger.LogInformation("ScriptEngine: 全部腳本執行完成，成功 {Ok}/{Total}",
            results.Count(r => r.Value.Success), results.Count);

        return results;
    }

    /// <summary>
    /// 執行指定模組的腳本清單。
    /// </summary>
    public async Task<Dictionary<string, ScriptResult>> RunModuleAsync(
        IReadOnlyList<(string Name, string Content)> moduleScripts,
        CancellationToken ct = default)
    {
        var results = new Dictionary<string, ScriptResult>();

        foreach (var (name, content) in moduleScripts)
        {
            if (ct.IsCancellationRequested) break;

            _logger.LogInformation("Collecting [{Script}]...", name);
            var result = await _executor.RunAsync(content, ct);

            if (result.Success)
                _logger.LogInformation("[{Script}] OK ({Length} chars)", name, result.RawOutput.Length);
            else
                _logger.LogWarning("[{Script}] FAILED: {Error}", name, result.ErrorMessage);

            results[name] = result;
        }

        return results;
    }
}
