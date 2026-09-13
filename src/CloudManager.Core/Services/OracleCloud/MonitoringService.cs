namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Monitoring;

using Oci.MonitoringService.Models;
using Oci.MonitoringService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class MonitoringService
{
    private readonly OciClientFactory factory;

    public MonitoringService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the alarms of the compartments in scope joined with their firing status
    public async ValueTask<List<AlarmInfo>> ListAlarmsAsync(CancellationToken cancellationToken = default)
    {
        using var monitoring = factory.CreateMonitoringClient();

        var alarms = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => monitoring.ListAlarms(new ListAlarmsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);
        var statuses = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => monitoring.ListAlarmsStatus(new ListAlarmsStatusRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);
        var statusById = statuses.ToDictionary(static x => x.Id, static x => x, StringComparer.Ordinal);

#pragma warning disable IDE0028
        return alarms
            .Select(x =>
            {
                var status = statusById.GetValueOrDefault(x.Id);
                return new AlarmInfo(
                    x.Id,
                    x.CompartmentId,
                    x.DisplayName,
                    x.Namespace,
                    x.Query,
                    OciValues.State(x.Severity),
                    x.IsEnabled ?? false,
                    OciValues.State(x.LifecycleState),
                    OciValues.State(status?.Status),
                    status?.TimestampTriggered);
            })
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Runs an MQL query (e.g. CpuUtilization[5m].mean()) and returns one series per resource.
    // Metrics live in the compartment of the resource; without one the query covers the selected compartment,
    // or the whole tenancy when the root is selected (the subtree option is only allowed on the root)
    public async ValueTask<List<MetricSeriesInfo>> GetMetricDataAsync(string namespaceName, string query, DateTime startTime, DateTime endTime, string? resolution = null, string? compartmentId = null, CancellationToken cancellationToken = default)
    {
        using var monitoring = factory.CreateMonitoringClient();
        var selected = factory.CompartmentId;
        var response = await monitoring.SummarizeMetricsData(
            new SummarizeMetricsDataRequest
            {
                CompartmentId = compartmentId ?? selected,
                CompartmentIdInSubtree = (compartmentId is null) && String.Equals(selected, factory.TenancyId, StringComparison.Ordinal),
                SummarizeMetricsDataDetails = new SummarizeMetricsDataDetails
                {
                    Namespace = namespaceName,
                    Query = query,
                    StartTime = startTime,
                    EndTime = endTime,
                    Resolution = resolution
                }
            },
            cancellationToken: cancellationToken);

#pragma warning disable IDE0028
        return response.Items
            .Select(static x => new MetricSeriesInfo(
                x.Name,
                x.Dimensions?.GetValueOrDefault("resourceId"),
                x.Dimensions?.GetValueOrDefault("resourceDisplayName"),
                x.AggregatedDatapoints
                    .Where(static p => p.Timestamp.HasValue)
                    .Select(static p => new MetricDataPoint(p.Timestamp!.Value, p.Value))
                    .OrderBy(static p => p.Timestamp)
                    .ToList()))
            .ToList();
#pragma warning restore IDE0028
    }
}
#pragma warning restore CA1724
