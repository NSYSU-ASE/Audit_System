using System;
using System.Collections.Generic;


namespace AseAudit.Core.Eetities
{
    public class AuditSourceModule
    {
        public int Id { get; set; } 
        public string Name { get; set; } // e.g., "身分識別模組"

        // 導覽屬性：一個模組包含多個評估流程 (Evaluation_list)
        public ICollection<EvaluationItem> Evaluations { get; set; }
    }

    public class EvaluationItem
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., "帳號管理"
        public ICollection<string> StandardId { get; set; } // e.g., "IEC-62443-3-3 SR 1.1"

        // FK: 屬於哪個模組
        public int ModuleId { get; set; }
        public AuditSourceModule Module { get; set; }

        // 導覽屬性：一個評估流程會產生多份材料/紀錄 (Material_list)
        // 注意：這通常代表「歷史紀錄」，每次稽核都會新增一筆 Material
        public ICollection<AuditMaterial> Materials { get; set; }
    }
    public class AuditMaterial
    {
        public long Id { get; set; } // 資料量大，建議用 long

        // FK: 屬於哪個評估流程
        public int EvaluationItemId { get; set; }
        public EvaluationItem EvaluationItem { get; set; }

        // FK: 是哪一台電腦回報的 (關聯到 Host 表)
        public int HostId { get; set; }
        // public Host Host { get; set; } // 若有 Host Entity 可加上

        // 核心資料：Agent 蒐集到的原始內容
        // e.g., 對於「帳號管理」，這裡可能存 {"Admin": true, "Guest": false}
        public string ValueJson { get; set; }

        public bool IsPass { get; set; } // 系統自動判定的結果 (通過/失敗)
        public DateTime CreatedAt { get; set; } // 蒐集時間
    }
}
