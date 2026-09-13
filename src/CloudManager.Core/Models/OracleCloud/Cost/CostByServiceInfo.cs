namespace CloudManager.Models.OracleCloud.Cost;

public sealed record CostByServiceInfo(
    string Service,
    decimal Amount,
    decimal Quantity,
    string Currency);
