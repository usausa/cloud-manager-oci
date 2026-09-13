namespace CloudManager.Models.OracleCloud.Monitoring;

// One series of a metric query, identified by the resource it was measured on
public sealed record MetricSeriesInfo(
    string Name,
    string? ResourceId,
    string? ResourceDisplayName,
    IReadOnlyList<MetricDataPoint> Points);
