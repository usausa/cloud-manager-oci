namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record InternetGatewayInfo(
    string Id,
    string DisplayName,
    bool IsEnabled,
    string State);
