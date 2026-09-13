namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Components.Controls;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// Shows Monitoring metrics of one resource as line charts
public sealed partial class MetricsDialog
{
    private List<ChartInfo> charts = [];

    private int hours = 1;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Namespace { get; set; } = string.Empty;

    [Parameter]
    public string ResourceId { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<string> Metrics { get; set; } = [];

    [Parameter]
    public string? Unit { get; set; }

    [Inject]
    public required MonitoringService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    // The resolution follows the period so that charts keep a similar point count
    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            var end = DateTime.UtcNow;
            var start = end.AddHours(-hours);
            var resolution = hours switch
            {
                <= 1 => "1m",
                <= 6 => "5m",
                _ => "15m"
            };

            var result = new List<ChartInfo>();
            foreach (var metric in Metrics)
            {
                var series = await Service.GetMetricDataAsync(Namespace, $"{metric}[{resolution}]{{resourceId = \"{ResourceId}\"}}.mean()", start, end, resolution, CancellationToken);
#pragma warning disable IDE0028
                result.Add(new ChartInfo(metric, series.Select(x => new MetricsChartSeries(x.ResourceDisplayName ?? metric, x.Points)).ToList()));
#pragma warning restore IDE0028
            }

            charts = result;
        });

    private sealed record ChartInfo(string Title, IReadOnlyList<MetricsChartSeries> Series);
}
