namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class FunctionsInvokeDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string FunctionName { get; set; } = string.Empty;

    private string payload = "{}";

    private string invokeType = FunctionsService.InvokeTypeSync;

    private void Submit() => MudDialog.Close(DialogResult.Ok(new FunctionsInvokeParams(payload, invokeType)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record FunctionsInvokeParams(string Payload, string InvokeType);
