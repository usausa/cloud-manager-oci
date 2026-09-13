namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class AdbConnectionDialog
{
    private List<ConnectionStringInfo> connectionStrings = [];

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string DatabaseId { get; set; } = string.Empty;

    [Parameter]
    public string DatabaseName { get; set; } = string.Empty;

    [Inject]
    public required AutonomousDatabaseService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            connectionStrings = await Service.GetConnectionStringsAsync(DatabaseId, CancellationToken);
        });
}
