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

    private Task OnTopicSelectedAsync(TopicInfo? topic)
    {
        selectedTopic = topic;
        subscriptions = [];
        if (topic is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            subscriptions = await Service.ListSubscriptionsAsync(topic.TopicId, CancellationToken);
        }, x => isSubscriptionsLoading = x);
    }

    private async Task PublishAsync(TopicInfo topic)
    {
        var dialog = await DialogService.ShowAsync<NotificationPublishDialog>("メッセージ発行", new DialogParameters<NotificationPublishDialog>
        {
            { x => x.TopicName, topic.Name }
        },
        Styles.MediumDialog);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (NotificationPublishParams)result.Data!;
        await RunAsync("発行中...", async (_, cancellationToken) =>
        {
            var messageId = await Service.PublishAsync(topic.TopicId, p.Title, p.Body, cancellationToken);
            Snackbar.AddSuccess($"メッセージを発行しました。(MessageId: {messageId})");
        });
    }
}
