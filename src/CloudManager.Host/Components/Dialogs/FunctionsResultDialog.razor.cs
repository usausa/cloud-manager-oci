namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class FunctionsResultDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    [Parameter]
    public string InvokeType { get; set; } = string.Empty;

    [Parameter]
    public string? RequestId { get; set; }

    [Parameter]
    public string Payload { get; set; } = string.Empty;

    private void Close() => MudDialog.Close();
}
