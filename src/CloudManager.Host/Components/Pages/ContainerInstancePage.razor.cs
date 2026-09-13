namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ContainerInstancePage
{
    private const int WaitTimeoutSeconds = 300;

    private List<ContainerInstanceInfo> instances = [];

    [Inject]
    public required ContainerInstanceService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            instances = await Service.ListAsync(CancellationToken);
        });

    private async Task StartAsync(ContainerInstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("起動", $"コンテナインスタンス {instance.DisplayName} を起動しますか？") is null)
        {
            return;
        }

        await RunAsync("起動中...", async (progress, cancellationToken) =>
        {
            await Service.StartAsync(instance.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.DisplayName} を起動しました。");
        }, LoadAsync);
    }

    private async Task StopAsync(ContainerInstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("停止", $"コンテナインスタンス {instance.DisplayName} を停止しますか？") is null)
        {
            return;
        }

        await RunAsync("停止中...", async (progress, cancellationToken) =>
        {
            await Service.StopAsync(instance.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.DisplayName} を停止しました。");
        }, LoadAsync);
    }

    private async Task RestartAsync(ContainerInstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("再起動", $"コンテナインスタンス {instance.DisplayName} を再起動しますか？") is null)
        {
            return;
        }

        await RunAsync("再起動中...", async (progress, cancellationToken) =>
        {
            await Service.RestartAsync(instance.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{instance.DisplayName} を再起動しました。");
        }, LoadAsync);
    }

    private async Task ShowContainersAsync(ContainerInstanceInfo instance)
    {
        await DialogService.ShowAsync<ContainersDialog>("コンテナ一覧", new DialogParameters<ContainersDialog>
        {
            { x => x.ContainerInstanceId, instance.Id },
            { x => x.InstanceName, instance.DisplayName }
        },
        Styles.MediumDialog);
    }

    private static Color StateColor(string state) => state switch
    {
        "ACTIVE" => Color.Success,
        "INACTIVE" => Color.Default,
        "CREATING" or "UPDATING" => Color.Warning,
        "DELETING" or "DELETED" or "FAILED" => Color.Error,
        _ => Color.Default
    };
}
