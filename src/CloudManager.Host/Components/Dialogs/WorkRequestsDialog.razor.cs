namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class WorkRequestsDialog
{
    private List<WorkRequestInfo> requests = [];

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string CompartmentId { get; set; } = string.Empty;

    [Parameter]
    public string ResourceId { get; set; } = string.Empty;

    [Parameter]
    public string ResourceName { get; set; } = string.Empty;

    [Inject]
    public required AutonomousDatabaseService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            requests = await Service.ListWorkRequestsAsync(CompartmentId, ResourceId, CancellationToken);
        });
}
