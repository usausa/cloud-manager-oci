namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record RouteTableInfo(
    string Id,
    string DisplayName,
    bool IsDefault,
    int RuleCount,
    string State);
