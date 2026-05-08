param(
    [string]$AccountCsv = "C:\Users\李元禎\Downloads\AM_Account.csv",
    [string]$RuleCsv = "C:\Users\李元禎\Downloads\AM_Rule.csv",
    [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=Ase_Audit;Trusted_Connection=True;TrustServerCertificate=True;",
    [switch]$Replace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Convert-DbNullToken {
    param(
        [AllowNull()]
        [string]$Value,
        [switch]$NotNullString
    )

    if ($null -eq $Value) {
        return $(if ($NotNullString) { "" } else { [DBNull]::Value })
    }

    $trimmed = $Value.Trim()
    if ($trimmed -eq "" -and $NotNullString) {
        return ""
    }

    if ($trimmed -eq "" -or $trimmed.ToUpperInvariant() -eq "NULL") {
        return [DBNull]::Value
    }

    return $Value
}

function Convert-Bit {
    param([AllowNull()][string]$Value)

    if ($null -eq $Value -or $Value.Trim() -eq "" -or $Value.Trim().ToUpperInvariant() -eq "NULL") {
        return [DBNull]::Value
    }

    return [bool]([int]$Value)
}

function Add-Param {
    param(
        [System.Data.SqlClient.SqlCommand]$Command,
        [string]$Name,
        [System.Data.SqlDbType]$Type,
        [object]$Value
    )

    $param = $Command.Parameters.Add($Name, $Type)
    $param.Value = $Value
    return $param
}

if (-not (Test-Path -LiteralPath $AccountCsv)) {
    throw "找不到 AM_Account CSV: $AccountCsv"
}

if (-not (Test-Path -LiteralPath $RuleCsv)) {
    throw "找不到 AM_Rule CSV: $RuleCsv"
}

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$connection.Open()
$transaction = $connection.BeginTransaction()

try {
    if ($Replace) {
        $delete = $connection.CreateCommand()
        $delete.Transaction = $transaction
        $delete.CommandText = @"
DELETE FROM dbo.Identification_AM_Account;
DELETE FROM dbo.Identification_AM_rule;
"@
        [void]$delete.ExecuteNonQuery()
    }

    $accountRows = Import-Csv -LiteralPath $AccountCsv -Header ID,CreatedTime,HostName,MACAddress,AccountName,Status,PasswordRequired
    $accountCount = 0

    foreach ($row in $accountRows) {
        $cmd = $connection.CreateCommand()
        $cmd.Transaction = $transaction
        $cmd.CommandText = @"
INSERT INTO dbo.Identification_AM_Account
    (CreatedTime, HostName, MACAddress, AccountName, Status, PasswordRequired)
VALUES
    (@CreatedTime, @HostName, @MACAddress, @AccountName, @Status, @PasswordRequired);
"@
        [void](Add-Param $cmd "@CreatedTime" ([System.Data.SqlDbType]::DateTime2) ([datetime]$row.CreatedTime))
        [void](Add-Param $cmd "@HostName" ([System.Data.SqlDbType]::NVarChar) $row.HostName)
        [void](Add-Param $cmd "@MACAddress" ([System.Data.SqlDbType]::NVarChar) (Convert-DbNullToken $row.MACAddress))
        [void](Add-Param $cmd "@AccountName" ([System.Data.SqlDbType]::NVarChar) $row.AccountName)
        [void](Add-Param $cmd "@Status" ([System.Data.SqlDbType]::NVarChar) (Convert-DbNullToken $row.Status))
        [void](Add-Param $cmd "@PasswordRequired" ([System.Data.SqlDbType]::Bit) (Convert-Bit $row.PasswordRequired))
        [void]$cmd.ExecuteNonQuery()
        $accountCount++
    }

    $ruleRows = Import-Csv -LiteralPath $RuleCsv -Header ID,CreatedTime,HostName,MACAddress,RestrictAnonymousSAM,EveryoneIncludesAnonymous,RestrictAnonymous,UserDomain,DomainRole
    $ruleCount = 0

    foreach ($row in $ruleRows) {
        $cmd = $connection.CreateCommand()
        $cmd.Transaction = $transaction
        $cmd.CommandText = @"
INSERT INTO dbo.Identification_AM_rule
    (CreatedTime, HostName, MACAddress, RestrictAnonymousSAM, EveryoneIncludesAnonymous, RestrictAnonymous, UserDomain, DomainRole)
VALUES
    (@CreatedTime, @HostName, @MACAddress, @RestrictAnonymousSAM, @EveryoneIncludesAnonymous, @RestrictAnonymous, @UserDomain, @DomainRole);
"@
        [void](Add-Param $cmd "@CreatedTime" ([System.Data.SqlDbType]::DateTime2) ([datetime]$row.CreatedTime))
        [void](Add-Param $cmd "@HostName" ([System.Data.SqlDbType]::NVarChar) $row.HostName)
        [void](Add-Param $cmd "@MACAddress" ([System.Data.SqlDbType]::NVarChar) (Convert-DbNullToken $row.MACAddress -NotNullString))
        [void](Add-Param $cmd "@RestrictAnonymousSAM" ([System.Data.SqlDbType]::Bit) (Convert-Bit $row.RestrictAnonymousSAM))
        [void](Add-Param $cmd "@EveryoneIncludesAnonymous" ([System.Data.SqlDbType]::Bit) (Convert-Bit $row.EveryoneIncludesAnonymous))
        [void](Add-Param $cmd "@RestrictAnonymous" ([System.Data.SqlDbType]::Bit) (Convert-Bit $row.RestrictAnonymous))
        [void](Add-Param $cmd "@UserDomain" ([System.Data.SqlDbType]::NVarChar) $row.UserDomain)
        [void](Add-Param $cmd "@DomainRole" ([System.Data.SqlDbType]::Int) ([int]$row.DomainRole))
        [void]$cmd.ExecuteNonQuery()
        $ruleCount++
    }

    $transaction.Commit()
    Write-Output "Imported Identification_AM_Account rows: $accountCount"
    Write-Output "Imported Identification_AM_rule rows: $ruleCount"
} catch {
    $transaction.Rollback()
    throw
} finally {
    $connection.Close()
}
