using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AseAudit.Collector
{
    /// <summary>
    /// 將 Worker 產生的 jsonContent 傳送至 appsettings 設定的中央 Server。
    /// 對應 Server 端 IngestController 的 POST /api/ingest/snapshot。
    /// </summary>
    public class SendJson
    {
        private const string ApiKeyHeader = "X-Audit-ApiKey";

        private readonly HttpClient _httpClient;
        private readonly ILogger<SendJson> _logger;
        private readonly string _snapshotEndpoint;
        private readonly string? _apiKey;

        public SendJson(HttpClient httpClient, IConfiguration configuration, ILogger<SendJson> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["Server:BaseUrl"]
                ?? throw new InvalidOperationException("appsettings.json 缺少 Server:BaseUrl 設定。");

            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            _snapshotEndpoint = configuration["Server:SnapshotEndpoint"] ?? "api/ingest/snapshot";
            _apiKey = configuration["Server:ApiKey"];

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// 將單一腳本的 jsonContent 傳送至 Server。
        /// </summary>
        /// <param name="scriptName">腳本名稱 (例如 HostAccountSnapshot)。</param>
        /// <param name="jsonContent">Worker 已序列化完成的 JSON 字串 (作為 payload)。</param>
        /// <param name="success">腳本是否執行成功。</param>
        /// <param name="errorMessage">失敗時的錯誤訊息。</param>
        public async Task<bool> SendAsync(
            string scriptName,
            string jsonContent,
            bool success,
            string? errorMessage,
            CancellationToken cancellationToken)
        {
            JsonElement payload;
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                payload = doc.RootElement.Clone();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "腳本 [{Script}] 的 jsonContent 不是合法 JSON，無法傳送。", scriptName);
                return false;
            }

            var upload = new
            {
                hostName = Environment.MachineName,
                hostId = (string?)null,
                scriptName,
                collectedAt = DateTime.UtcNow,
                success,
                errorMessage,
                payload
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _snapshotEndpoint)
            {
                Content = JsonContent.Create(upload)
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add(ApiKeyHeader, _apiKey);
            }

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning(
                        "傳送腳本 [{Script}] 失敗，HTTP {Status}: {Body}",
                        scriptName, (int)response.StatusCode, body);
                    return false;
                }

                _logger.LogInformation(
                    "腳本 [{Script}] 已成功傳送至 {Endpoint}",
                    scriptName, new Uri(_httpClient.BaseAddress!, _snapshotEndpoint));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "傳送腳本 [{Script}] 發生例外。", scriptName);
                return false;
            }
        }
    }
}
