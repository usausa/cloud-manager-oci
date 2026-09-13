namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class AdbBackupCreateDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string DatabaseName { get; set; } = string.Empty;

    private string displayName = string.Empty;

    protected override void OnInitialized()
    {
        displayName = $"{DatabaseName}-{DateTime.Now:yyyyMMdd-HHmm}";
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(displayName.Trim()));
}
