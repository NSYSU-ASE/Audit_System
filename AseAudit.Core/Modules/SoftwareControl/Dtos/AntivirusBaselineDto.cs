using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.SoftwareRecognition.Dtos
{
    /// <summary>
    /// 防毒基準（由對方維護輸入：最新版本 / 允許版本範圍）
    /// </summary>
    public sealed class AntivirusBaselineDto
    {
        public string ProductName { get; set; } = "Antivirus";

        // ✅ 最新版本由他們輸入（你只接 DB 的值，不要寫死）
        public string LatestVersion { get; set; } = "";

        // 可選：病毒碼/定義檔基準。未提供時不強制判定病毒碼版本。
        public string? LatestDefinitionVersion { get; set; }
        public int? MaxDefinitionAgeDays { get; set; }

        // 可選：允許落後的版本（例如允許落後一個小版本）
        public bool AllowGreaterOrEqual { get; set; } = true;

        // 可選：如果他們想用「白名單版本」而不是單一最新版
        public List<string> AllowedVersions { get; set; } = new();
    }
}
