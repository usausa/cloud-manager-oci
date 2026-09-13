namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ObjectStoragePage
{
    private List<BucketInfo> buckets = [];

    private string? namespaceName;

    [Inject]
    public required ObjectStorageService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadBucketsAsync();

    protected override Task OnSessionChangedAsync() => LoadBucketsAsync();

    private Task LoadBucketsAsync() =>
        LoadAsync(async () =>
        {
            namespaceName = await Service.GetNamespaceAsync(CancellationToken);
            buckets = await Service.ListBucketsAsync(CancellationToken);
        });

    private async Task ShowAccessAsync(BucketInfo bucket)
    {
        await DialogService.ShowAsync<BucketAccessDialog>("可視性 / 設定", new DialogParameters<BucketAccessDialog>
        {
            { x => x.BucketName, bucket.Name }
        });
    }

    private async Task ShowLifecycleAsync(BucketInfo bucket)
    {
        await DialogService.ShowAsync<ObjectLifecycleDialog>("ライフサイクルルール", new DialogParameters<ObjectLifecycleDialog>
        {
            { x => x.BucketName, bucket.Name }
        },
        Styles.MediumDialog);
    }
}
