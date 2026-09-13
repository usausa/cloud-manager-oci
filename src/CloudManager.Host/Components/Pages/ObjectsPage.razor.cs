namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ObjectsPage
{
    private const long MaxUploadBytes = 1024L * 1024 * 1024;

    private List<ObjectRow> rows = [];

    private string prefix = string.Empty;

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Inject]
    public required ObjectStorageService Service { get; set; }

    protected override Task OnParametersSetAsync()
    {
        rows = [];
        return LoadObjectsAsync();
    }

    protected override Task OnSessionChangedAsync() => LoadObjectsAsync();

    // Prefixes are shown as folders above the objects of the current level
    private Task LoadObjectsAsync() =>
        LoadAsync(async () =>
        {
            var listing = await Service.ListObjectsWithDelimiterAsync(BucketName, prefix, CancellationToken);
            var result = listing.Prefixes.Select(x => new ObjectRow(x, Relative(x), true, null)).ToList();
            result.AddRange(listing.Objects.Where(x => x.Name != prefix).Select(x => new ObjectRow(x.Name, Relative(x.Name), false, x)));
            rows = result;
        });

    private Task EnterPrefixAsync(string name)
    {
        prefix = name;
        return LoadObjectsAsync();
    }

    private Task GoUpAsync()
    {
        var trimmed = prefix.TrimEnd('/');
        var index = trimmed.LastIndexOf('/');
        prefix = index < 0 ? string.Empty : trimmed[..(index + 1)];
        return LoadObjectsAsync();
    }

    private string Relative(string name) =>
        name.StartsWith(prefix, StringComparison.Ordinal) ? name[prefix.Length..] : name;

    private async Task UploadAsync()
    {
        var dialog = await DialogService.ShowAsync<ObjectUploadDialog>("アップロード", new DialogParameters<ObjectUploadDialog>
        {
            { x => x.BucketName, BucketName },
            { x => x.Prefix, prefix }
        });
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var uploadParams = (ObjectUploadParams)dialogResult.Data!;
        await RunAsync("アップロード中...", async (progress, cancellationToken) =>
        {
            await using var stream = uploadParams.File.OpenReadStream(MaxUploadBytes, cancellationToken);
            await Service.UploadStreamAsync(BucketName, uploadParams.Name, stream, progress, cancellationToken);
            Snackbar.AddSuccess($"{uploadParams.Name} をアップロードしました。");
        }, LoadObjectsAsync);
    }

    // The download API has no circuit session, so the profile is passed in the URL
    private string DownloadUrl(ObjectInfo obj) =>
        $"api/objectstorage/download/{Uri.EscapeDataString(BucketName)}?name={Uri.EscapeDataString(obj.Name)}&profile={Uri.EscapeDataString(Session.ProfileName)}&region={Uri.EscapeDataString(Session.Region?.RegionId ?? string.Empty)}";

    private async Task DeleteObjectAsync(ObjectInfo obj)
    {
        if (await DialogService.ShowOperationConfirm("削除", $"オブジェクト {obj.Name} を削除しますか？", requireConfirmText: obj.Name.Split('/').Last()) is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteObjectAsync(BucketName, obj.Name, cancellationToken);
            Snackbar.AddSuccess($"{obj.Name} を削除しました。");
        }, LoadObjectsAsync);
    }

    private async Task DeletePrefixAsync(string name)
    {
        var message = $"プレフィックス {name} 配下のオブジェクトをすべて削除します。この操作は取り消せません。確認のためプレフィックス名を入力してください。";
        if (await DialogService.ShowOperationConfirm("一括削除", message, requireConfirmText: name.TrimEnd('/').Split('/').Last()) is null)
        {
            return;
        }

        await RunAsync("削除中...", async (progress, cancellationToken) =>
        {
            await Service.DeleteObjectsByPrefixAsync(BucketName, name, progress, cancellationToken);
            Snackbar.AddSuccess($"{name} を削除しました。");
        }, LoadObjectsAsync);
    }

    private async Task ShowVersionsAsync(ObjectInfo obj)
    {
        await DialogService.ShowAsync<ObjectVersionsDialog>("バージョン履歴", new DialogParameters<ObjectVersionsDialog>
        {
            { x => x.BucketName, BucketName },
            { x => x.ObjectName, obj.Name }
        },
        Styles.MediumDialog);
    }

    private async Task CopyMoveAsync(ObjectInfo obj)
    {
        var dialog = await DialogService.ShowAsync<ObjectCopyDialog>("コピー / 移動", new DialogParameters<ObjectCopyDialog>
        {
            { x => x.SourceBucket, BucketName },
            { x => x.SourceName, obj.Name }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (ObjectCopyParams)result.Data!;
        await RunAsync(p.Move ? "移動中..." : "コピー中...", async (_, cancellationToken) =>
        {
            if (p.Move)
            {
                await Service.MoveObjectAsync(BucketName, obj.Name, p.DestinationBucket, p.DestinationName, cancellationToken);
            }
            else
            {
                await Service.CopyObjectAsync(BucketName, obj.Name, p.DestinationBucket, p.DestinationName, cancellationToken);
            }

            Snackbar.AddSuccess(p.Move ? "移動しました。" : "コピーしました。");
        }, LoadObjectsAsync);
    }

    private async Task PreviewAsync(ObjectInfo obj)
    {
        await DialogService.ShowAsync<ObjectPreviewDialog>("プレビュー", new DialogParameters<ObjectPreviewDialog>
        {
            { x => x.BucketName, BucketName },
            { x => x.Target, obj }
        },
        Styles.LargeDialog);
    }

    private sealed record ObjectRow(string Name, string DisplayName, bool IsPrefix, ObjectInfo? Object);
}
