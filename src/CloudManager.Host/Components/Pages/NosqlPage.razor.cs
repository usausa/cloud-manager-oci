namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class NosqlPage
{
    private static readonly DialogOptions WideDialogOptions = new() { MaxWidth = MaxWidth.Large, FullWidth = true, CloseOnEscapeKey = true };

    private List<NosqlTableInfo> tables = [];

    [Inject]
    public required NosqlService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            tables = await Service.ListTablesAsync(CancellationToken);
        });

    private async Task QueryAsync(NosqlTableInfo table)
    {
        await DialogService.ShowAsync<NosqlQueryDialog>(
            string.Empty,
            new DialogParameters<NosqlQueryDialog>
            {
                { x => x.TableName, table.Name }
            },
            WideDialogOptions);
    }

    private async Task ShowSchemaAsync(NosqlTableInfo table)
    {
        await DialogService.ShowAsync<NosqlSchemaDialog>("スキーマ", new DialogParameters<NosqlSchemaDialog>
        {
            { x => x.TableName, table.Name }
        });
    }
}
