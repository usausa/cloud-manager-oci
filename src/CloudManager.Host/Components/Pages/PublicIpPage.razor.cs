namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class PublicIpPage
{
    private List<PublicIpInfo> addresses = [];

    [Inject]
    public required PublicIpService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            addresses = await Service.ListPublicIpsAsync(CancellationToken);
        });

    private async Task AssignAsync(PublicIpInfo publicIp)
    {
        var dialog = await DialogService.ShowAsync<PublicIpAssignDialog>("パブリック IP 割当", new DialogParameters<PublicIpAssignDialog>
        {
            { x => x.IpAddress, publicIp.IpAddress }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var instanceId = (string)result.Data!;
        await RunAsync("割当中...", async (_, cancellationToken) =>
        {
            await Service.AssignAsync(publicIp.Id, instanceId, cancellationToken);
            Snackbar.AddSuccess("パブリック IP を割り当てました。");
        }, LoadAsync);
    }

    private async Task UnassignAsync(PublicIpInfo publicIp)
    {
        if (await DialogService.ShowOperationConfirm("割当解除", $"パブリック IP {publicIp.IpAddress} の割当を解除しますか？") is null)
        {
            return;
        }

        await RunAsync("割当解除中...", async (_, cancellationToken) =>
        {
            await Service.UnassignAsync(publicIp.Id, cancellationToken);
            Snackbar.AddSuccess("割当を解除しました。");
        }, LoadAsync);
    }
}
