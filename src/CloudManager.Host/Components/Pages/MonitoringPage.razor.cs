namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Controls;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class MonitoringPage
{
    private List<AlarmInfo> alarms = [];

    private string searchText = string.Empty;

    private string metricNamespace = "oci_computeagent";

    private string metricQuery = "CpuUtilization[5m].mean()";

    private int hours = 1;

    private bool isMetricsLoading;

    private bool metricsQueried;

    private List<MetricsChartSeries> metricSeries = [];

    [Inject]
    public required MonitoringService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync()
    {
        metricsQueried = false;
        metricSeries = [];
        return LoadAsync();
    }

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            alarms = await Service.ListAlarmsAsync(CancellationToken);
        });

    private bool FilterFunc(AlarmInfo alarm) =>
        String.IsNullOrWhiteSpace(searchText) || alarm.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static Color AlarmStatusColor(string status) => status switch
    {
        "OK" => Color.Success,
        "FIRING" => Color.Error,
        "SUSPENDED" => Color.Warning,
        _ => Color.Default
    };

    // One series per resource; the resolution is left to the query interval
    private async Task QueryMetricsAsync()
    {
        isMetricsLoading = true;
        ErrorMessage = null;
        try
        {
            var end = DateTime.UtcNow;
            var series = await Service.GetMetricDataAsync(metricNamespace.Trim(), metricQuery.Trim(), end.AddHours(-hours), end, null, null, CancellationToken);
#pragma warning disable IDE0028
            metricSeries = series
                .Select(x => new MetricsChartSeries(x.ResourceDisplayName ?? x.ResourceId ?? x.Name, x.Points))
                .ToList();
#pragma warning restore IDE0028
            metricsQueried = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isMetricsLoading = false;
        }
    }
}
