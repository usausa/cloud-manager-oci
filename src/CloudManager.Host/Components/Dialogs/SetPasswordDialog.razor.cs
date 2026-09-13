namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class SetPasswordDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string UserName { get; set; } = string.Empty;

    private string password = string.Empty;

    private string confirm = string.Empty;

    private void Submit() => MudDialog.Close(DialogResult.Ok(password));

    private void Cancel() => MudDialog.Cancel();
}
