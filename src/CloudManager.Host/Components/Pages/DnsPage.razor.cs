namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DnsPage
{
    private List<DnsZoneInfo> zones = [];

    private List<DnsRecordInfo> records = [];

    private DnsZoneInfo? selectedZone;

    private string searchText = string.Empty;

    private bool isRecordsLoading;

    [Inject]
    public required DnsService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedZone = null;
        records = [];
        return LoadAsync(async () =>
        {
            zones = await Service.ListZonesAsync(CancellationToken);
        });
    }

    private bool FilterFunc(DnsZoneInfo zone) =>
        String.IsNullOrWhiteSpace(searchText) || zone.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private Task OnZoneSelectedAsync(DnsZoneInfo? zone)
    {
        selectedZone = zone;
        records = [];
        return zone is null ? Task.CompletedTask : LoadRecordsAsync();
    }

    private Task LoadRecordsAsync()
    {
        if (selectedZone is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            records = await Service.ListRecordsAsync(selectedZone.Id, selectedZone.Scope, CancellationToken);
        }, x => isRecordsLoading = x);
    }

    // SOA and NS at the apex are managed by the service
    private bool IsProtected(DnsRecordInfo record) =>
        (record.Rtype is "SOA" or "NS") && (selectedZone is not null) && String.Equals(record.Domain, selectedZone.Name, StringComparison.OrdinalIgnoreCase);

    private async Task OpenUpsertDialogAsync(DnsRecordInfo? record)
    {
        if (selectedZone is null)
        {
            return;
        }

        var dialog = await DialogService.ShowAsync<DnsRecordDialog>("レコード追加/更新", new DialogParameters<DnsRecordDialog>
        {
            { x => x.ZoneName, selectedZone.Name },
            { x => x.Record, record }
        },
        Styles.MediumDialog);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (DnsRecordParams)result.Data!;
        await RunAsync("保存中...", async (_, cancellationToken) =>
        {
            await Service.UpsertRecordAsync(selectedZone.Id, selectedZone.Scope, p.Domain, p.Rtype, p.Ttl, p.Values, cancellationToken);
            Snackbar.AddSuccess($"{p.Domain} を保存しました。");
            await LoadRecordsAsync();
        });
    }

    private async Task DeleteRecordAsync(DnsRecordInfo record)
    {
        if (selectedZone is null)
        {
            return;
        }

        if (await DialogService.ShowOperationConfirm("レコード削除", $"レコード「{record.Domain} ({record.Rtype})」を削除しますか？") is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteRecordAsync(selectedZone.Id, selectedZone.Scope, record.Domain, record.Rtype, cancellationToken);
            Snackbar.AddSuccess($"{record.Domain} を削除しました。");
            await LoadRecordsAsync();
        });
    }
}
