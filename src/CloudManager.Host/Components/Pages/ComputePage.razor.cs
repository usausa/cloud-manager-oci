namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ComputePage
{
    private const int WaitTimeoutSeconds = 300;

    private static readonly string[] States = ["RUNNING", "STOPPED", "STARTING", "STOPPING", "PROVISIONING", "TERMINATING", "TERMINATED"];

    private static readonly string[] MetricNames = ["CpuUtilization", "MemoryUtilization"];

    private List<ComputeInstanceInfo> instances = [];

    private string? filterState;

    private string filterTag = string.Empty;

    private RunCommandResult? commandResult;

    [Inject]
    public required ComputeService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            instances = await Service.ListInstancesAsync(filterState, filterTag, CancellationToken);
        });

    private async Task StartAsync(ComputeInstanceInfo instance)
    {
        if (await DialogService.ShowOperationConfirm("Start", $"インスタンス {instance.DisplayName} を起動しますか？") is null)
        {
            return;
        }

        await RunAsync("起動中...", async (progress, cancellationToken) =>
        {
            await Service.StartAsync(instance.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"Start 完了: {instance.DisplayName}");
        }, LoadAsync);
    }

    private async Task StopAsync(ComputeInstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("Stop", $"インスタンス {instance.DisplayName} を停止しますか？(強制指定なしは SOFTSTOP)", showForce: true);
        if (result is null)
        {
            return;
        }

        await RunAsync("停止中...", async (progress, cancellationToken) =>
        {
            await Service.StopAsync(instance.Id, result.Force, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"Stop 完了: {instance.DisplayName}");
        }, LoadAsync);
    }

    private async Task RebootAsync(ComputeInstanceInfo instance)
    {
        var result = await DialogService.ShowOperationConfirm("Reboot", $"インスタンス {instance.DisplayName} を再起動しますか？(強制指定なしは SOFTRESET)", showForce: true);
        if (result is null)
        {
            return;
        }

        await RunAsync("再起動中...", async (_, cancellationToken) =>
        {
            await Service.RebootAsync(instance.Id, result.Force, cancellationToken);
            Snackbar.AddSuccess($"Reboot 完了: {instance.DisplayName}");
        }, LoadAsync);
    }

    private async Task TerminateAsync(ComputeInstanceInfo instance)
    {
        var message = $"インスタンス {instance.DisplayName} をブートボリュームごと終了(削除)します。この操作は取り消せません。確認のためインスタンス名を入力してください。";
        if (await DialogService.ShowOperationConfirm("Terminate", message, requireConfirmText: instance.DisplayName) is null)
        {
            return;
        }

        await RunAsync("終了中...", async (progress, cancellationToken) =>
        {
            await Service.TerminateAsync(instance.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"Terminate 完了: {instance.DisplayName}");
        }, LoadAsync);
    }

    private async Task RunCommandAsync(ComputeInstanceInfo instance)
    {
        var dialog = await DialogService.ShowAsync<RunCommandDialog>(
            "Run Command",
            new DialogParameters<RunCommandDialog>
            {
                { x => x.InstanceName, instance.DisplayName }
            });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var parameters = (RunCommandParams)result.Data!;
        await RunAsync("コマンド実行中...", async (progress, cancellationToken) =>
        {
            commandResult = await Service.RunCommandAsync(instance.Id, parameters.Command, parameters.TimeoutSeconds, progress, cancellationToken);
            if (commandResult.Status == "SUCCEEDED")
            {
                Snackbar.AddSuccess($"コマンド完了: {instance.DisplayName}");
            }
            else
            {
                Snackbar.AddWarning($"コマンド終了 [{commandResult.Status}]: {instance.DisplayName}");
            }
        });
    }

    private async Task ShowMetricsAsync(ComputeInstanceInfo instance)
    {
        await DialogService.ShowAsync<MetricsDialog>(
            string.Empty,
            new DialogParameters<MetricsDialog>
            {
                { x => x.Title, instance.DisplayName },
                { x => x.Namespace, "oci_computeagent" },
                { x => x.ResourceId, instance.Id },
                { x => x.Metrics, MetricNames },
                { x => x.Unit, "%" }
            },
            new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseOnEscapeKey = true });
    }

    private static Color StateColor(string state) => state switch
    {
        "RUNNING" => Color.Success,
        "STOPPED" => Color.Default,
        "STARTING" or "STOPPING" or "PROVISIONING" or "TERMINATING" => Color.Warning,
        "TERMINATED" => Color.Error,
        _ => Color.Default
    };
}
