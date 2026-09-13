namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class NotificationsPage
{
    private List<TopicInfo> topics = [];

    private List<SubscriptionInfo> subscriptions = [];

    private TopicInfo? selectedTopic;

    private bool isSubscriptionsLoading;

    [Inject]
    public required NotificationsService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedTopic = null;
        subscriptions = [];
        return LoadAsync(async () =>
        {
            topics = await Service.ListTopicsAsync(CancellationToken);
        });
    }

    private async Task OnTopicSelectedAsync(TopicInfo? topic)
    {
        selectedTopic = topic;
        subscriptions = [];
        if (topic is null)
        {
            return;
        }

        isSubscriptionsLoading = true;
        try
        {
            subscriptions = await Service.ListSubscriptionsAsync(topic.TopicId, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isSubscriptionsLoading = false;
        }
    }

    private async Task PublishAsync(TopicInfo topic)
    {
        var dialog = await DialogService.ShowAsync<NotificationPublishDialog>("メッセージ発行", new DialogParameters<NotificationPublishDialog>
        {
            { x => x.TopicName, topic.Name }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (NotificationPublishParams)result.Data!;
        await RunAsync("発行中...", async (_, cancellationToken) =>
        {
            var messageId = await Service.PublishAsync(topic.TopicId, p.Title, p.Body, cancellationToken);
            Snackbar.AddSuccess($"発行完了 (MessageId: {messageId})");
        });
    }
}
