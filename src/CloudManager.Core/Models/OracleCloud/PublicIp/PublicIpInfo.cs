namespace CloudManager.Models.OracleCloud.PublicIp;

public sealed record PublicIpInfo(
    string Id,
    string DisplayName,
    string IpAddress,
    string State,
    string Lifetime,
    string? AssignedEntityId,
    string? AssignedEntityType,
    string? PrivateIpId,
    DateTime TimeCreated);
