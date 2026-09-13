namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class NosqlSchemaDialog
{
    private NosqlTableDetail? detail;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string CompartmentId { get; set; } = string.Empty;

    [Parameter]
    public string TableName { get; set; } = string.Empty;

    [Inject]
    public required NosqlService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            detail = await Service.GetTableAsync(CompartmentId, TableName, CancellationToken);
        });
}
