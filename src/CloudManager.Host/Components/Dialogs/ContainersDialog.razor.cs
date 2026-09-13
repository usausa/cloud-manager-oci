namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ContainersDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string ContainerInstanceId { get; set; } = string.Empty;

    [Parameter]
    public string InstanceName { get; set; } = string.Empty;

    [Inject]
    public required ContainerInstanceService Service { get; set; }

    private List<ContainerInfo> containers = [];

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            containers = await Service.ListContainersAsync(ContainerInstanceId, CancellationToken);
        });
}
