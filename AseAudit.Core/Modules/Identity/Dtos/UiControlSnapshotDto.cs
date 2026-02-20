namespace AseAudit.Core.Modules.Identity.Dtos;

public sealed class UiControlSnapshotDto
{
    // OCR 後的文字（整段/多行都可）
    public string OcrText { get; init; } = string.Empty;

    // 來源畫面名稱（方便 Debug）
    public string ScreenName { get; init; } = "Unknown";
}
