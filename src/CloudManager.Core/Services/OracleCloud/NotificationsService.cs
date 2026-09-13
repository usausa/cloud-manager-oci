namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Notifications;

using Oci.OnsService.Models;
using Oci.OnsService.Requests;

public sealed class NotificationsService
{
    private readonly OciClientFactory factory;

    public NotificationsService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the topics of the compartments in scope with their subscription counts
    public async ValueTask<List<TopicInfo>> ListTopicsAsync(CancellationToken cancellationToken = default)
    {
        using var control = factory.CreateNotificationControlPlaneClient();
        using var data = factory.CreateNotificationDataPlaneClient();

        var topics = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => control.ListTopics(new ListTopicsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);
        var subscriptions = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => data.ListSubscriptions(new ListSubscriptionsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);
        var countByTopic = subscriptions
            .GroupBy(static x => x.TopicId, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.Count(), StringComparer.Ordinal);

#pragma warning disable IDE0028
        return topics
            .Select(x => new TopicInfo(
                x.TopicId,
                x.CompartmentId,
                x.Name,
                OciValues.State(x.LifecycleState),
                x.Description,
                countByTopic.GetValueOrDefault(x.TopicId),
                x.TimeCreated))
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Subscriptions of a topic; they may live in any compartment in scope
    public async ValueTask<List<SubscriptionInfo>> ListSubscriptionsAsync(string topicId, CancellationToken cancellationToken = default)
    {
        using var data = factory.CreateNotificationDataPlaneClient();
        var subscriptions = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => data.ListSubscriptions(new ListSubscriptionsRequest { CompartmentId = compartmentId, TopicId = topicId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);

#pragma warning disable IDE0028
        return subscriptions
            .Select(static x => new SubscriptionInfo(x.Id, x.Protocol, x.Endpoint, OciValues.State(x.LifecycleState), x.CreatedTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(x.CreatedTime.Value).UtcDateTime : null))
            .OrderBy(static x => x.Endpoint, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Publishes a message and returns its id
    public async ValueTask<string> PublishAsync(string topicId, string? title, string body, CancellationToken cancellationToken = default)
    {
        using var data = factory.CreateNotificationDataPlaneClient();
        var response = await data.PublishMessage(
            new PublishMessageRequest
            {
                TopicId = topicId,
                MessageDetails = new MessageDetails { Title = String.IsNullOrWhiteSpace(title) ? null : title, Body = body }
            },
            cancellationToken: cancellationToken);
        return response.PublishResult.MessageId;
    }
}
