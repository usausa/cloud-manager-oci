namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class Home
{
    private static readonly ServiceCategory[] Categories =
    [
        new("コンピュート",
        [
            new("Compute", "compute", Icons.Material.Filled.Computer),
            new("Block Volume", "block-volume", Icons.Material.Filled.Album),
            new("Container Instances", "container-instances", Icons.Material.Filled.AccountTree),
            new("Functions", "functions", Icons.Material.Filled.Functions)
        ]),
        new("コンテナ",
        [
            new("Container Registry", "ocir", Icons.Material.Filled.Inventory2)
        ]),
        new("ストレージ / DB",
        [
            new("Object Storage", "object-storage", Icons.Material.Filled.Folder),
            new("Autonomous Database", "adb", Icons.Material.Filled.Storage),
            new("NoSQL", "nosql", Icons.Material.Filled.TableChart)
        ]),
        new("ネットワーク",
        [
            new("VCN", "vcn", Icons.Material.Filled.Lan),
            new("Public IP", "public-ip", Icons.Material.Filled.Language),
            new("Load Balancer", "load-balancer", Icons.Material.Filled.Balance),
            new("DNS", "dns", Icons.Material.Filled.Dns),
            new("Certificates", "certificates", Icons.Material.Filled.Lock)
        ]),
        new("API / 統合",
        [
            new("API Gateway", "api-gateway", Icons.Material.Filled.Api),
            new("Events", "events", Icons.Material.Filled.EventNote)
        ]),
        new("メッセージング",
        [
            new("Queue", "queue", Icons.Material.Filled.Mail),
            new("Notifications", "notifications", Icons.Material.Filled.NotificationsActive)
        ]),
        new("監視",
        [
            new("Monitoring", "monitoring", Icons.Material.Filled.Timeline),
            new("Logging", "logging", Icons.Material.Filled.Article)
        ]),
        new("セキュリティ / ID",
        [
            new("Vault", "vault", Icons.Material.Filled.VpnKey),
            new("Identity Domains", "identity-domains", Icons.Material.Filled.People),
            new("Bastion", "bastion", Icons.Material.Filled.Terminal)
        ]),
        new("ジョブ",
        [
            new("ジョブ一覧", "jobs", Icons.Material.Filled.EventNote),
            new("実行履歴", "jobs/history", Icons.Material.Filled.History)
        ]),
        new("その他",
        [
            new("リソース検索", "resource-search", Icons.Material.Filled.Search),
            new("Cost", "cost", Icons.Material.Filled.AttachMoney),
            new("設定", "settings", Icons.Material.Filled.Settings)
        ])
    ];

    private int computeRunning;

    private int computeStopped;

    private int adbAvailable;

    private int adbStopped;

    private int containerActive;

    private int containerInactive;

    private int alarmFiring;

    private decimal? costCurrentMonth;

    private decimal? costPreviousMonth;

    private string costCurrency = string.Empty;

    private string? costError;

    [Inject]
    public required ResourceSearchService ResourceSearchService { get; set; }

    [Inject]
    public required MonitoringService MonitoringService { get; set; }

    [Inject]
    public required CostService CostService { get; set; }

    // The tenancy root shows the whole tenancy, other compartments only themselves
    private bool IsTenancyScope => String.Equals(Session.CompartmentId, Session.TenancyId, StringComparison.Ordinal);

    private string ScopeName => IsTenancyScope ? $"{Session.CompartmentName} (テナンシ全体)" : Session.CompartmentName;

    private static string CurrentMonthLabel => DateTime.UtcNow.ToString("yyyy/MM", CultureInfo.InvariantCulture);

    private static string PreviousMonthLabel => DateTime.UtcNow.AddMonths(-1).ToString("yyyy/MM", CultureInfo.InvariantCulture);

    protected override Task OnInitializedAsync() =>
        Session.IsProfileAvailable ? LoadAsync(LoadSummaryAsync) : Task.CompletedTask;

    protected override Task OnSessionChangedAsync() =>
        Session.IsProfileAvailable ? LoadAsync(LoadSummaryAsync) : Task.CompletedTask;

    private async Task LoadSummaryAsync()
    {
        // The base class has loaded the compartments, so the counts follow the same scope as the pages
        var countTask = ResourceSearchService.CountByStateAsync(["instance", "autonomousdatabase", "containerinstance"], IsTenancyScope ? null : Session.ScopeCompartmentIds, CancellationToken).AsTask();
        var alarmTask = MonitoringService.ListAlarmsAsync(CancellationToken).AsTask();
        var costTask = LoadCostAsync();
        await Task.WhenAll(countTask, alarmTask, costTask);

        var counts = await countTask;
        computeRunning = Count(counts, "Instance", "RUNNING");
        computeStopped = Count(counts, "Instance", "STOPPED");
        adbAvailable = Count(counts, "AutonomousDatabase", "AVAILABLE");
        adbStopped = Count(counts, "AutonomousDatabase", "STOPPED");
        containerActive = Count(counts, "ContainerInstance", "ACTIVE");
        containerInactive = Count(counts, "ContainerInstance", "INACTIVE");

        alarmFiring = (await alarmTask).Count(static x => x.Status == "FIRING");
    }

    // Cost failures (e.g. missing usage-report permissions) only affect the cost card
    private async Task LoadCostAsync()
    {
        costCurrentMonth = null;
        costPreviousMonth = null;
        costError = null;
        try
        {
            var compartmentId = IsTenancyScope ? null : Session.CompartmentId;
            var now = DateTime.UtcNow;
            var previous = now.AddMonths(-1);
            var current = await CostService.SummarizeByServiceAsync(now.Year, now.Month, compartmentId, CancellationToken);
            var last = await CostService.SummarizeByServiceAsync(previous.Year, previous.Month, compartmentId, CancellationToken);

            costCurrentMonth = current.Sum(static x => x.Amount);
            costPreviousMonth = last.Sum(static x => x.Amount);
            costCurrency = current.Concat(last).Select(static x => x.Currency).FirstOrDefault(static x => !String.IsNullOrEmpty(x)) ?? string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            costError = FormatError(ex);
        }
    }

    private static string FormatAmount(decimal? value) =>
        value?.ToString("N2", CultureInfo.InvariantCulture) ?? "-";

    private static int Count(IEnumerable<ResourceStateCount> counts, string resourceType, string state) =>
        counts
            .Where(x => String.Equals(x.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase) && (x.State == state))
            .Sum(static x => x.Count);

    private sealed record ServiceCategory(string Name, ServiceItem[] Items);

    private sealed record ServiceItem(string Name, string Href, string Icon);
}
