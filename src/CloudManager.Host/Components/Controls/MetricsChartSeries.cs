namespace CloudManager.Host.Components.Controls;

public sealed record MetricsChartSeries(string Label, IReadOnlyList<MetricDataPoint> Points);
