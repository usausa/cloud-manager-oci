namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class BlockVolumePage
{
    private List<BlockVolumeInfo> volumes = [];

    private string kindFilter = string.Empty;

    private string stateFilter = string.Empty;

    [Inject]
    public required BlockVolumeService Service { get; set; }

    private IEnumerable<BlockVolumeInfo> FilteredVolumes =>
        volumes
            .Where(x => kindFilter switch
            {
                "block" => !x.IsBootVolume,
                "boot" => x.IsBootVolume,
                _ => true
            })
            .Where(x => String.IsNullOrEmpty(stateFilter) || (x.State == stateFilter));

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            volumes = await Service.ListVolumesAsync(null, CancellationToken);
        });

    private async Task CreateBackupAsync(BlockVolumeInfo volume)
    {
        var dialog = await DialogService.ShowAsync<VolumeBackupDialog>("バックアップ作成", new DialogParameters<VolumeBackupDialog>
        {
            { x => x.VolumeName, volume.DisplayName }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var displayName = (string)result.Data!;
        await RunAsync("バックアップ作成中...", async (_, cancellationToken) =>
        {
            var id = await Service.CreateBackupAsync(volume.Id, volume.IsBootVolume, displayName, cancellationToken);
            Snackbar.AddSuccess($"バックアップ {DisplayFormat.Ocid(id)} を作成しました。");
        });
    }

    private async Task AttachAsync(BlockVolumeInfo volume)
    {
        var dialog = await DialogService.ShowAsync<VolumeAttachDialog>("ボリュームのアタッチ", new DialogParameters<VolumeAttachDialog>
        {
            { x => x.VolumeName, volume.DisplayName }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (VolumeAttachParams)result.Data!;
        await RunAsync("アタッチ中...", async (_, cancellationToken) =>
        {
            await Service.AttachAsync(volume.Id, p.InstanceId, p.AttachmentType, p.Device, cancellationToken);
            Snackbar.AddSuccess("ボリュームをアタッチしました。");
        }, LoadAsync);
    }

    private async Task DetachAsync(BlockVolumeInfo volume)
    {
        if (await DialogService.ShowOperationConfirm("デタッチ", $"ボリューム {volume.DisplayName} をデタッチしますか？確認のためボリューム名を入力してください。", requireConfirmText: volume.DisplayName) is null)
        {
            return;
        }

        await RunAsync("デタッチ中...", async (_, cancellationToken) =>
        {
            await Service.DetachAsync(volume.AttachmentId!, cancellationToken);
            Snackbar.AddSuccess("ボリュームをデタッチしました。");
        }, LoadAsync);
    }

    private static Color StateColor(string state) => state switch
    {
        "AVAILABLE" => Color.Success,
        "PROVISIONING" or "RESTORING" => Color.Warning,
        "TERMINATING" or "TERMINATED" or "FAULTY" => Color.Error,
        _ => Color.Default
    };
}
