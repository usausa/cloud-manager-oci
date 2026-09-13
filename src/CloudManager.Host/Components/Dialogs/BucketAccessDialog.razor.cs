namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class BucketAccessDialog
{
    private BucketAccessInfo? access;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Inject]
    public required ObjectStorageService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            access = await Service.GetBucketAccessAsync(BucketName, CancellationToken);
        });
}
