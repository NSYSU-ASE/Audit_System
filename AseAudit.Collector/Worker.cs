using System.Text.Json;

namespace AseAudit.Collector
{
    public class Worker : BackgroundService
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

            var scriptResults = await _scriptEngine.RunAllAsync(stoppingToken);

            // 將 ScriptResult 轉換為可序列化的 JSON 物件
            var allResults = new Dictionary<string, object>();
            foreach (var (name, result) in scriptResults)
            {
                if (result.Success)
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(result.RawOutput);
                        allResults[name] = jsonDoc.RootElement.Clone();
                    }
                    catch (JsonException)
                    {
                        allResults[name] = result.RawOutput;
                    }
                }
                else
                {
                    allResults[name] = new { Error = result.ErrorMessage ?? "Unknown error" };
                }
            }

            // Serialize the results to a JSON file
            var outputFilePath = Path.Combine(AppContext.BaseDirectory, "audit_results.json");
            try
            {
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                var jsonResult = JsonSerializer.Serialize(allResults, jsonOptions);
                await File.WriteAllTextAsync(outputFilePath, jsonResult, stoppingToken);
                _logger.LogInformation("Audit collection complete. Results saved to: {FilePath}", outputFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write results to JSON file.");
            }
            finally
            {
                _hostLifetime.StopApplication();
            }
        }
    }
}
