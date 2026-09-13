namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ObjectLifecycleDialog
{
    private List<ObjectLifecycleRuleInfo> rules = [];

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Inject]
    public required ObjectStorageService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            rules = await Service.GetLifecycleAsync(BucketName, CancellationToken);
        });
}
