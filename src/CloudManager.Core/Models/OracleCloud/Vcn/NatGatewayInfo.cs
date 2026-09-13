namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record NatGatewayInfo(
    string Id,
    string DisplayName,
    string? NatIp,
    bool BlockTraffic,
    string State);
