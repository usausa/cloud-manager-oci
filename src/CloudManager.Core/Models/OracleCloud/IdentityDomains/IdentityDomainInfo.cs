namespace CloudManager.Models.OracleCloud.IdentityDomains;

// Endpoint is the domain URL used for the SCIM APIs
public sealed record IdentityDomainInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string Endpoint,
    string Type,
    string LicenseType,
    string State,
    string? HomeRegion,
    DateTime? TimeCreated);
