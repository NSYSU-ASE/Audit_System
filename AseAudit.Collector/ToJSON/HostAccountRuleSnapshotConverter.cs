using System.Text.Json;
using System.Text.Json.Serialization;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 將 HostAccountRuleSnapshot 的 PowerShell 原始輸出 (僅 Payload 內容) 組裝為
/// 完整的 <see cref="HostAccountRuleSnapshotPayload"/>：先反序列化為
/// <see cref="HostAccountRuleSnapshotContent"/>，再填入 <see cref="HostInfo"/>
/// 取得的 HostId / Hostname，確保 JSON 結構與 Shared Contract 對齊。
///
/// 由於 PowerShell 以 <c>@(Get-ItemProperty ...)</c> 強制陣列包裝，
/// 匿名存取欄位原始 JSON 會是 <c>[1]</c> 或 <c>[]</c>；
/// 透過 <see cref="NullableIntArrayOrScalarConverter"/> 自動拆封為純量 / null。
///
/// 後續由 API 將其寫入資料表 <see cref="HostAccountRuleSnapshotPayload.TableName"/>
/// (Identification_AM_rule) 單列。
/// </summary>
public class HostAccountRuleSnapshotConverter : IScriptJsonConverter
{
    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new NullableIntArrayOrScalarConverter() }
    };

    public string ToJson(string rawOutput, HostInfo hostInfo, JsonSerializerOptions options)
    {
        var content = JsonSerializer.Deserialize<HostAccountRuleSnapshotContent>(rawOutput, _deserializeOptions)
                      ?? new HostAccountRuleSnapshotContent();

        var payload = new HostAccountRuleSnapshotPayload
        {
            HostId = hostInfo.HostId,
            Hostname = hostInfo.Hostname,
            Payload = content,
            ScriptName = HostAccountRuleSnapshotPayload.Script
        };

        return JsonSerializer.Serialize(payload, options);
    }
}

/// <summary>
/// 將 <c>[1]</c> / <c>[]</c> / <c>1</c> / <c>null</c> 統一轉為 <see cref="int?"/>。
/// 空陣列視為 null（對應登錄機碼不存在的情境）。
/// </summary>
internal sealed class NullableIntArrayOrScalarConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.GetInt32();
            case JsonTokenType.StartArray:
                int? value = null;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray) break;
                    if (reader.TokenType == JsonTokenType.Number) value = reader.GetInt32();
                }
                return value;
            default:
                throw new JsonException($"Unexpected token {reader.TokenType} for nullable int.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}
