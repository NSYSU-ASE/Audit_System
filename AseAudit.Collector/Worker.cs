using System.Text.Json;
using AseAudit.Collector.Script_lib;
using AseAudit.Collector.ToJSON;

namespace AseAudit.Collector
{
    public partial class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ScriptEngine _scriptEngine;
        private readonly IHostApplicationLifetime _hostLifetime;

        public Worker(ILogger<Worker> logger, ScriptEngine scriptEngine, IHostApplicationLifetime hostLifetime)
        {
            _logger = logger;
            _scriptEngine = scriptEngine;
            _hostLifetime = hostLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit collection starting.");

            // ★ 測試模式：執行 EventLogSnapshot 與 EventStatusSnapshot，各自輸出獨立 JSON
            var testScripts = new List<(string Name, string Content)>
            {
                (nameof(EventStatusSnapshot), EventStatusSnapshot.Content),
                (nameof(HostAccountRuleSnapshot), HostAccountRuleSnapshot.Content),
                (nameof(HostAccountSnapshot), HostAccountSnapshot.Content)
            };
            var scriptResults = await _scriptEngine.RunModuleAsync(testScripts, stoppingToken);

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            try
            {
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
                                jsonContent = converter!.ToJson(result.RawOutput, jsonOptions);
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
    }
}
