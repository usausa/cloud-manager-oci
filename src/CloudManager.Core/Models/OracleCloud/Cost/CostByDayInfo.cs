namespace CloudManager.Models.OracleCloud.Cost;

public sealed record CostByDayInfo(
    DateTime Day,
    decimal Amount,
    string Currency);
