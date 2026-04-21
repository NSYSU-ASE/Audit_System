using System.Text.Json;
using AseAudit.Collector.Script_lib;
using AseAudit.Collector.ToJSON;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Collector
{
    public partial class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ScriptEngine _scriptEngine;
        private readonly IHostApplicationLifetime _hostLifetime;
        private readonly SendJson _sendJson;

        public Worker(
            ILogger<Worker> logger,
            ScriptEngine scriptEngine,
            IHostApplicationLifetime hostLifetime,
            SendJson sendJson)
        {
            _logger = logger;
            _scriptEngine = scriptEngine;
            _hostLifetime = hostLifetime;
            _sendJson = sendJson;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit collection starting.");

            // ── ① 先執行共同腳本取得主機識別資訊 ──────────────────
            var hostInfo = await CollectHostInfoAsync(stoppingToken);
            _logger.LogInformation(
                "HostInfo collected: HostId={HostId}, Hostname={Hostname}",
                hostInfo.HostId, hostInfo.Hostname);

            // ── ② 執行各 payload 腳本（各自只產出 Payload 內容） ───
            var payloadScripts = new List<(string Name, string Content)>
            {
                (nameof(EventStatusSnapshot), EventStatusSnapshot.Content),
                (HostAccountRuleSnapshotPayload.Script, HostAccountRuleSnapshot.Content),
                (HostAccountSnapshotPayload.Script, HostAccountSnapshot.Content)
            };
            var scriptResults = await _scriptEngine.RunModuleAsync(payloadScripts, stoppingToken);

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            try
            {
                // ── ③ 由 Converter 組裝成 Contract Payload 格式並輸出／發送 ─
                foreach (var (name, result) in scriptResults)
                {
                    var outputFilePath = Path.Combine(AppContext.BaseDirectory, $"{name}.json");
                    try
                    {
                        string jsonContent;
                        if (result.Success)
                        {
                            if (JsonConverterRegistry.TryGet(name, out var converter))
                            {
                                jsonContent = converter!.ToJson(result.RawOutput, hostInfo, jsonOptions);
                            }
                            else
                            {
                                try
                                {
                                    var jsonDoc = JsonDocument.Parse(result.RawOutput);
                                    jsonContent = JsonSerializer.Serialize(jsonDoc.RootElement, jsonOptions);
                                }
                                catch (JsonException)
                                {
                                    jsonContent = JsonSerializer.Serialize(new { result.RawOutput }, jsonOptions);
                                }
                            }
                        }
                        else
                        {
                            jsonContent = JsonSerializer.Serialize(new { Error = result.ErrorMessage ?? "Unknown error" }, jsonOptions);
                        }

                        await File.WriteAllTextAsync(outputFilePath, jsonContent, stoppingToken);
                        _logger.LogInformation("Script [{Name}] saved to: {FilePath}", name, outputFilePath);

                        await _sendJson.SendAsync(
                            name,
                            jsonContent,
                            result.Success,
                            result.ErrorMessage,
                            stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to write results for script [{Name}].", name);
                    }
                }
            }
            finally
            {
                _hostLifetime.StopApplication();
            }
        }

        /// <summary>
        /// 執行共同腳本 <see cref="HostInfoSnapshot"/> 取得主機識別資訊。
        /// 腳本失敗或輸出無法解析時，退回使用 <see cref="HostInfo.FromEnvironment"/>。
        /// </summary>
        private async Task<HostInfo> CollectHostInfoAsync(CancellationToken stoppingToken)
        {
            var results = await _scriptEngine.RunModuleAsync(
                new[] { (nameof(HostInfoSnapshot), HostInfoSnapshot.Content) },
                stoppingToken);

            if (!results.TryGetValue(nameof(HostInfoSnapshot), out var result) || !result.Success)
            {
                _logger.LogWarning("HostInfoSnapshot 執行失敗，退回使用 Environment.MachineName。");
                return HostInfo.FromEnvironment();
            }

            try
            {
                var info = result.Deserialize<HostInfo>();
                if (info is null ||
                    string.IsNullOrWhiteSpace(info.HostId) ||
                    string.IsNullOrWhiteSpace(info.Hostname))
                {
                    _logger.LogWarning("HostInfoSnapshot 輸出欄位缺漏，退回使用 Environment.MachineName。");
                    return HostInfo.FromEnvironment();
                }
                return info;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "HostInfoSnapshot 輸出非合法 JSON，退回使用 Environment.MachineName。");
                return HostInfo.FromEnvironment();
            }
        }
    }
}
