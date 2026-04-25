using AseAudit.Core.Entities;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Infrastructure.Mapping;

public static class IdentityEntityMapper
{
    public static HostAccountSnapshotDto ToHostAccountSnapshotDto(this IdentificationAmAccount entity)
    {
        var accountName = entity.AccountName ?? "";
        var isAd = accountName.Contains("\\") || accountName.Contains("@");

        return new HostAccountSnapshotDto
        {
            HostId = entity.HostName,
            Hostname = entity.HostName,

            HasAd = isAd,
            IsAdAccount = isAd,

            IsLocalAdmin = accountName.Contains("admin", StringComparison.OrdinalIgnoreCase),

            LoginAccount = entity.AccountName
        };
    }

    public static HostIdentitySnapshotDto ToHostIdentitySnapshotDto(this IdentificationAmAccount entity)
    {
        var isAd = entity.AccountName?.Contains("\\") == true || entity.AccountName?.Contains("@") == true;

        return new HostIdentitySnapshotDto
        {
            LoggedInAdAccount = isAd ? entity.AccountName : null
        };
    }

    public static EmployeeDirectoryRecordDto ToEmployeeDirectoryRecordDto(this IdentificationAmAccount entity)
    {
        var status = entity.Status ?? "";

        return new EmployeeDirectoryRecordDto
        {
            AdAccount = entity.AccountName ?? "",
            IsActive =
                status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("在職") ||
                status.Contains("啟用")
        };
    }

    public static PasswordPolicySnapshotDto ToPasswordPolicySnapshotDto(this IdentificationAmRule entity)
    {
        return new PasswordPolicySnapshotDto
        {
            RawNetAccountsText =
                $"Host={entity.HostName}; Domain={entity.UserDomain}; DomainRole={entity.DomainRole}; " +
                $"RestrictAnonymousSAM={entity.RestrictAnonymousSAM}; " +
                $"RestrictAnonymous={entity.RestrictAnonymous}; " +
                $"EveryoneIncludesAnonymous={entity.EveryoneIncludesAnonymous}"
        };
    }
}