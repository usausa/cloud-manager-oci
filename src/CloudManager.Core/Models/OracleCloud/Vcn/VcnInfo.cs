namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record VcnInfo(
    string Id,
    string DisplayName,
    string CidrBlocks,
    string State,
    string? DnsLabel,
    DateTime TimeCreated);
