namespace CloudManager.Models.OracleCloud.IdentityDomains;

public sealed record DomainUserInfo(
    string Id,
    string UserName,
    string? DisplayName,
    bool Active,
    string? Email,
    DateTime? Created);
