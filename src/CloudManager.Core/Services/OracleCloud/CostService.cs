namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Cost;

using Oci.UsageapiService.Models;
using Oci.UsageapiService.Requests;

public sealed class CostService
{
    private readonly OciClientFactory factory;

    public CostService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Cost of one calendar month grouped by service, optionally of one compartment only
    public async ValueTask<List<CostByServiceInfo>> SummarizeByServiceAsync(int year, int month, string? compartmentId, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var items = await RequestAsync(start, start.AddMonths(1), RequestSummarizedUsagesDetails.GranularityEnum.Monthly, compartmentId, cancellationToken);

#pragma warning disable IDE0028
        return items
            .GroupBy(static x => (Service: x.Service ?? "-", Currency: x.Currency ?? string.Empty))
            .Select(static g => new CostByServiceInfo(g.Key.Service, g.Sum(static x => x.ComputedAmount ?? 0m), g.Sum(static x => x.ComputedQuantity ?? 0m), g.Key.Currency))
            .Where(static x => x.Amount != 0)
            .OrderByDescending(static x => x.Amount)
            .ToList();
#pragma warning restore IDE0028
    }

    // Daily cost totals between two UTC dates (end exclusive)
    public async ValueTask<List<CostByDayInfo>> SummarizeByDayAsync(DateTime start, DateTime end, string? compartmentId, CancellationToken cancellationToken = default)
    {
        var items = await RequestAsync(start.Date, end.Date, RequestSummarizedUsagesDetails.GranularityEnum.Daily, compartmentId, cancellationToken);

#pragma warning disable IDE0028
        return items
            .Where(static x => x.TimeUsageStarted.HasValue)
            .GroupBy(static x => (Day: x.TimeUsageStarted!.Value.Date, Currency: x.Currency ?? string.Empty))
            .Select(static g => new CostByDayInfo(g.Key.Day, g.Sum(static x => x.ComputedAmount ?? 0m), g.Key.Currency))
            .OrderBy(static x => x.Day)
            .ToList();
#pragma warning restore IDE0028
    }

    private async ValueTask<List<UsageSummary>> RequestAsync(DateTime start, DateTime end, RequestSummarizedUsagesDetails.GranularityEnum granularity, string? compartmentId, CancellationToken cancellationToken)
    {
        using var usage = factory.CreateUsageapiClient();
        var details = new RequestSummarizedUsagesDetails
        {
            TenantId = factory.TenancyId,
            TimeUsageStarted = DateTime.SpecifyKind(start, DateTimeKind.Utc),
            TimeUsageEnded = DateTime.SpecifyKind(end, DateTimeKind.Utc),
            Granularity = granularity,
            QueryType = RequestSummarizedUsagesDetails.QueryTypeEnum.Cost,
            GroupBy = ["service"]
        };
        if (!String.IsNullOrEmpty(compartmentId))
        {
            details.Filter = new Filter
            {
                Operator = Filter.OperatorEnum.And,
                Dimensions = [new Dimension { Key = "compartmentId", Value = compartmentId }]
            };
        }

        return await OciPaging.ListAllAsync(
            page => usage.RequestSummarizedUsages(new RequestSummarizedUsagesRequest { RequestSummarizedUsagesDetails = details, Page = page }, cancellationToken: cancellationToken),
            static x => x.UsageAggregation.Items,
            static x => x.OpcNextPage);
    }
}
