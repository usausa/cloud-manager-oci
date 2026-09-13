namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class FunctionsConcurrencyDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionId { get; set; } = string.Empty;

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Parameter]
    public int? Current { get; set; }

    [Inject]
    public required FunctionsService Service { get; set; }

    private int? newValue;

    private bool isSaving;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        newValue = Current;
    }

    private async Task SaveAsync()
    {
        isSaving = true;
        try
        {
            await Service.SetProvisionedConcurrencyAsync(FunctionId, newValue, CancellationToken);
            Snackbar.AddSuccess("プロビジョニング済み同時実行を更新しました。");
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
}
