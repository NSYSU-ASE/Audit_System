using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using Dapper;
using AseAudit.Core.Entities;

namespace AseAudit.Infrastructure.Repositories;

public class IdentityRepository
{
    private readonly IDbConnection _conn;

    public IdentityRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    /// <summary>
    /// 取得帳號資料
    /// </summary>
    public IEnumerable<IdentificationAmAccount> GetAccounts()
    {
        return _conn.Query<IdentificationAmAccount>(
            "SELECT * FROM Identification_AM_Account");
    }

    /// <summary>
    /// 取得規則資料（密碼政策）
    /// </summary>
    public IEnumerable<IdentificationAmRule> GetRules()
    {
        return _conn.Query<IdentificationAmRule>(
            "SELECT * FROM Identification_AM_rule");
    }
}