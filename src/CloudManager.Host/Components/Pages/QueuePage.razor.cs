namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class QueuePage
{
    private const int ReceiveLimit = 10;

    private const int ReceiveVisibilitySeconds = 30;

    private List<QueueInfo> queues = [];

    private string searchText = string.Empty;

    [Inject]
    public required QueueService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            queues = await Service.ListQueuesAsync(CancellationToken);
        });

    private bool FilterFunc(QueueInfo queue) =>
        String.IsNullOrWhiteSpace(searchText) || queue.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private static bool IsActive(QueueInfo queue) => queue.State == "ACTIVE";

    private static Color StateColor(string state) => state switch
    {
        "ACTIVE" => Color.Success,
        "CREATING" or "UPDATING" => Color.Info,
        "DELETING" or "DELETED" or "FAILED" => Color.Error,
        _ => Color.Default
    };

    private async Task SendMessageAsync(QueueInfo queue)
    {
        var dialog = await DialogService.ShowAsync<QueueSendMessageDialog>("メッセージ送信", new DialogParameters<QueueSendMessageDialog>
        {
            { x => x.QueueName, queue.DisplayName }
        },
        Styles.MediumDialog);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (QueueSendMessageParams)result.Data!;
        await RunAsync("送信中...", async (_, cancellationToken) =>
        {
            await Service.PutMessageAsync(queue.Id, queue.MessagesEndpoint, p.Content, cancellationToken);
            Snackbar.AddSuccess($"{queue.DisplayName} にメッセージを送信しました。");
            await LoadAsync();
        });
    }

    private async Task ReceiveMessagesAsync(QueueInfo queue)
    {
        List<QueueMessageInfo>? messages = null;
        await RunAsync("受信中...", async (_, cancellationToken) =>
        {
            messages = await Service.GetMessagesAsync(queue.Id, queue.MessagesEndpoint, ReceiveLimit, ReceiveVisibilitySeconds, cancellationToken);
        });
        if (messages is null)
        {
            return;
        }

        // Messages deleted from the dialog are removed from the queue
        var dialog = await DialogService.ShowAsync<QueueMessagesDialog>("受信メッセージ", new DialogParameters<QueueMessagesDialog>
        {
            { x => x.QueueId, queue.Id },
            { x => x.QueueName, queue.DisplayName },
            { x => x.MessagesEndpoint, queue.MessagesEndpoint },
            { x => x.Messages, messages }
        },
        Styles.LargeDialog);
        await dialog.Result;
        await LoadAsync();
    }

    private async Task PurgeAsync(QueueInfo queue)
    {
        if (await DialogService.ShowOperationConfirm("パージ", $"キュー「{queue.DisplayName}」のメッセージ (デッドレターキューを含む) をすべて削除します。", requireConfirmText: queue.DisplayName) is null)
        {
            return;
        }

        await RunAsync("パージ中...", async (_, cancellationToken) =>
        {
            await Service.PurgeQueueAsync(queue.Id, cancellationToken);
            Snackbar.AddSuccess($"{queue.DisplayName} をパージしました。");
            await LoadAsync();
        });
    }
}
