namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ObjectVersionsDialog
{
    private List<ObjectVersionInfo> versions = [];

    private bool isRestoring;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Parameter]
    public string ObjectName { get; set; } = string.Empty;

    [Inject]
    public required ObjectStorageService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            versions = await Service.ListVersionsAsync(BucketName, ObjectName, CancellationToken);
        });

    private async Task RestoreAsync(ObjectVersionInfo version)
    {
        isRestoring = true;
        try
        {
            await Service.RestoreVersionAsync(BucketName, ObjectName, version.VersionId, CancellationToken);
            Snackbar.AddSuccess($"バージョン {version.VersionId} を復元しました。");
            versions = await Service.ListVersionsAsync(BucketName, ObjectName, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isRestoring = false;
        }
    }
}
