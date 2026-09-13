namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class NosqlQueryDialog
{
    private List<NosqlRowInfo> rows = [];

    private List<string> columns = [];

    private string statement = string.Empty;

    private int limit = 100;

    private bool executed;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string CompartmentId { get; set; } = string.Empty;

    [Parameter]
    public string TableName { get; set; } = string.Empty;

    [Inject]
    public required NosqlService Service { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        statement = $"SELECT * FROM {TableName}";
    }

    // Columns are the union of keys so that sparse rows still line up
    private Task RunAsync() =>
        LoadAsync(async () =>
        {
            rows = await Service.QueryAsync(CompartmentId, statement, limit, CancellationToken);
#pragma warning disable IDE0028
            columns = rows.SelectMany(static x => x.Columns.Keys).Distinct(StringComparer.Ordinal).ToList();
#pragma warning restore IDE0028
            executed = true;
        });
}
