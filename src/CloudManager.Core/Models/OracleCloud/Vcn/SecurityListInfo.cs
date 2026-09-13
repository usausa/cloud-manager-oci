namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record SecurityListInfo(
    string Id,
    string DisplayName,
    bool IsDefault,
    int IngressCount,
    int EgressCount,
    string State);
