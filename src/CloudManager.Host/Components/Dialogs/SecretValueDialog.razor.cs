namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// The value stays masked until the user reveals it
public sealed partial class SecretValueDialog
{
    private const string Masked = "••••••••••••";

    private bool reveal;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public SecretValueInfo Value { get; set; } = new(string.Empty, string.Empty, null, null, null);

    private void Close() => MudDialog.Close();
}
