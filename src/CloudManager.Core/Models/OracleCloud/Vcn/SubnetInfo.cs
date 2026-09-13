namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record SubnetInfo(
    string Id,
    string DisplayName,
    string CidrBlock,
    string? AvailabilityDomain,
    bool IsPublic,
    string State);
