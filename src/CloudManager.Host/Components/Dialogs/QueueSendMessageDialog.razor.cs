namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class QueueSendMessageDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string QueueName { get; set; } = string.Empty;

    private string content = string.Empty;

    private void Submit() => MudDialog.Close(DialogResult.Ok(new QueueSendMessageParams(content)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record QueueSendMessageParams(string Content);
