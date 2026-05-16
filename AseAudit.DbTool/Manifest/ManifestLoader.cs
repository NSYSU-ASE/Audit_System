using System.Text.Json;
using System.Text.RegularExpressions;

namespace AseAudit.DbTool.Manifest;

public sealed class ManifestLoader
{
    private static readonly Regex SafeNamePattern = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public ManifestFile Load(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            throw new ManifestException($"找不到 manifest 檔：{manifestPath}");

        ManifestFile? manifest;
        try
        {
            var json = File.ReadAllText(manifestPath);
            manifest = JsonSerializer.Deserialize<ManifestFile>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ManifestException($"manifest JSON 格式錯誤：{ex.Message}", ex);
        }

        if (manifest is null)
            throw new ManifestException("manifest 內容為空");

        Validate(manifest, manifestPath);
        return manifest;
    }

    private static void Validate(ManifestFile manifest, string manifestPath)
    {
        if (manifest.Tables is null || manifest.Tables.Count == 0)
            throw new ManifestException("manifest 至少需有一張表");

        var nameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var loadOrderSet = new HashSet<int>();
        var manifestDir = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        var scriptRootAbs = Path.GetFullPath(Path.Combine(manifestDir, manifest.ScriptRoot));

        foreach (var t in manifest.Tables)
        {
            if (!SafeNamePattern.IsMatch(t.Name))
                throw new ManifestException($"表名含不合法字元：{t.Name}");

            if (!nameSet.Add(t.Name))
                throw new ManifestException($"表名重複：{t.Name}");

            if (!loadOrderSet.Add(t.LoadOrder))
                throw new ManifestException($"loadOrder 重複：{t.LoadOrder}（表 {t.Name}）");

            var scriptAbs = Path.Combine(scriptRootAbs, t.CreateScript);
            if (!File.Exists(scriptAbs))
                throw new ManifestException(
                    $"表 {t.Name} 的 createScript 不存在：{scriptAbs}");
        }
    }
}
