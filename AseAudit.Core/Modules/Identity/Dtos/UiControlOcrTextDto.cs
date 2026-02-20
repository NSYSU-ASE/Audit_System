namespace AseAudit.Core.Modules.Identity.Dtos;

public sealed class UiControlOcrTextDto
{
    // 直接貼 OCR 出來的文字（Swagger 測試用）
    public string Text { get; init; } = string.Empty;

    // 可選：畫面名稱
    public string ScreenName { get; init; } = "Unknown";
}
