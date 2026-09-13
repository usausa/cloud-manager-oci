namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Controls;

using Microsoft.AspNetCore.Components;

public sealed partial class CostPage
{
    private static readonly int[] Years = Enumerable.Range(DateTime.UtcNow.Year - 2, 3).ToArray();

    private static readonly int[] Months = Enumerable.Range(1, 12).ToArray();

    private List<CostByServiceInfo> byService = [];

    private List<MetricsChartSeries> dailySeries = [];

    private decimal total;

    private string currency = string.Empty;

    private int year = DateTime.UtcNow.Year;

    private int month = DateTime.UtcNow.Month;

    private bool compartmentOnly;

    [Inject]
    public required CostService Service { get; set; }

    private bool IsTenancyScope => String.Equals(Session.CompartmentId, Session.TenancyId, StringComparison.Ordinal);

    protected override Task OnInitializedAsync()
    {
        compartmentOnly = !IsTenancyScope;
        return LoadAsync();
    }

    protected override Task OnSessionChangedAsync()
    {
        compartmentOnly = !IsTenancyScope;
        return LoadAsync();
    }

    // The daily series is charted with dates as labels; timestamps are day boundaries in UTC
    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            var compartmentId = compartmentOnly ? Session.CompartmentId : null;
            var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

            byService = await Service.SummarizeByServiceAsync(year, month, compartmentId, CancellationToken);
            var byDay = await Service.SummarizeByDayAsync(start, start.AddMonths(1), compartmentId, CancellationToken);

            total = byService.Sum(static x => x.Amount);
            currency = byService.Select(static x => x.Currency).FirstOrDefault(static x => !String.IsNullOrEmpty(x)) ?? string.Empty;
#pragma warning disable IDE0028
            dailySeries =
            [
                new MetricsChartSeries(
                    "コスト",
                    byDay.Select(static x => new MetricDataPoint(DateTime.SpecifyKind(x.Day, DateTimeKind.Utc).AddHours(12), (double)x.Amount)).ToList())
            ];
#pragma warning restore IDE0028
        });

    private Task OnYearChangedAsync(int value)
    {
        year = value;
        return LoadAsync();
    }

    private Task OnMonthChangedAsync(int value)
    {
        month = value;
        return LoadAsync();
    }

    private Task OnScopeChangedAsync(bool value)
    {
        compartmentOnly = value;
        return LoadAsync();
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
