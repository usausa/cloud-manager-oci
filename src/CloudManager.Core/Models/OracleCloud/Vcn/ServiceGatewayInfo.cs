namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record ServiceGatewayInfo(
    string Id,
    string DisplayName,
    string Services,
    string State);
