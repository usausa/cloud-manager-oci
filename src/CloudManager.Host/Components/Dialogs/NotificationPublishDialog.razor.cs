namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class NotificationPublishDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string TopicName { get; set; } = string.Empty;

    private string? title;

    private string body = string.Empty;

    private void Submit() => MudDialog.Close(DialogResult.Ok(new NotificationPublishParams(title, body)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record NotificationPublishParams(string? Title, string Body);
