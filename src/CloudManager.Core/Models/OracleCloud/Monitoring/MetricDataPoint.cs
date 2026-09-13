namespace CloudManager.Models.OracleCloud.Monitoring;

public sealed record MetricDataPoint(
    DateTime Timestamp,
    double Value);
