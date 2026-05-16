using System;

namespace AseAudit.Core.Modules.Software.Dtos
{
    [Obsolete("Use AuthorizedSoftwareBlacklistEntryDto for SR1.2 blacklist matching.")]
    public sealed class AuthorizedSoftwareWhitelistEntryDto : AuthorizedSoftwareBlacklistEntryDto
    {
    }
}
