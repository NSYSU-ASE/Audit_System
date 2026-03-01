using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.SoftwareRecognition.Dtos
{
    /// <summary>
    /// 每台機台防毒狀態（資料由對方 DB 提供）
    /// </summary>
    public sealed class AntivirusStatusRecordDto
    {
        public string DeviceId { get; set; } = "";          // 機台識別碼/HostName/AssetId 之類
        public bool IsInstalled { get; set; }               // 是否已安裝防毒

        // TODO: 你忘記版本欄位名稱的話，請把下面這行改成「他們資料庫實際的版本欄位」
        // 例：AgentVersion / ProductVersion / EngineVersion / DefinitionVersion ... 等
        public string? InstalledVersion { get; set; }       // ←【要改欄位名就改這個 property】

        // 可選：如果他們 DB 有狀態欄位（正常/停用/過期/異常碼）
        public string? Status { get; set; }                 // e.g. "OK", "Disabled", "Expired"
    }
}
