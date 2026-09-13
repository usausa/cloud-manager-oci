namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class FunctionsConfigDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionId { get; set; } = string.Empty;

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Inject]
    public required FunctionsService Service { get; set; }

    private List<ConfigEntry> entries = [];

    private bool isSaving;

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            var config = await Service.GetConfigAsync(FunctionId, CancellationToken);
#pragma warning disable IDE0028
            entries = config.Select(static x => new ConfigEntry { Key = x.Key, Value = x.Value }).ToList();
#pragma warning restore IDE0028
        });

    private async Task SaveAsync()
    {
        var config = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries.Where(static x => !String.IsNullOrWhiteSpace(x.Key)))
        {
            config[entry.Key.Trim()] = entry.Value;
        }

        isSaving = true;
        try
        {
            await Service.UpdateConfigAsync(FunctionId, config, CancellationToken);
            Snackbar.AddSuccess("構成を更新しました。");
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isSaving = false;
        }
    }

    // Editable row bound to the table
    private sealed class ConfigEntry
    {
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }
}
