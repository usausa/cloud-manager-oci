namespace CloudManager.Host.Components.Dialogs;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class QueueMessagesDialog
{
    private List<QueueMessageInfo> messages = [];

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string QueueId { get; set; } = string.Empty;

    [Parameter]
    public string QueueName { get; set; } = string.Empty;

    [Parameter]
    public string MessagesEndpoint { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<QueueMessageInfo> Messages { get; set; } = [];

    [Inject]
    public required QueueService Service { get; set; }

    protected override void OnParametersSet()
    {
        messages = [.. Messages];
    }

    private Task DeleteAsync(QueueMessageInfo message) =>
        RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteMessageAsync(QueueId, MessagesEndpoint, message.Receipt, cancellationToken);
            messages.Remove(message);
            Snackbar.AddSuccess($"メッセージ {message.Id} を削除しました。");
        });

    private void Close() => MudDialog.Close();
}
