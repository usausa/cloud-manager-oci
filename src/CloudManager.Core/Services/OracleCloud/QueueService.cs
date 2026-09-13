namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Queue;

using Oci.QueueService.Models;
using Oci.QueueService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class QueueService
{
    private readonly OciClientFactory factory;

    public QueueService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the queues of the compartments in scope with their settings and statistics
    public async ValueTask<List<QueueInfo>> ListQueuesAsync(CancellationToken cancellationToken = default)
    {
        using var admin = factory.CreateQueueAdminClient();
        var queues = await factory.ListInScopeAsync(
            admin,
            (client, compartmentId, page) => client.ListQueues(new ListQueuesRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.QueueCollection.Items,
            static x => x.OpcNextPage,
            cancellationToken);

        var result = new List<QueueInfo>();
        foreach (var summary in queues)
        {
            var queue = (await admin.GetQueue(new GetQueueRequest { QueueId = summary.Id }, cancellationToken: cancellationToken)).Queue;

            Stats? stats = null;
            if (!String.IsNullOrEmpty(queue.MessagesEndpoint))
            {
                using var client = factory.CreateQueueClient(queue.MessagesEndpoint);
                stats = (await client.GetStats(new GetStatsRequest { QueueId = queue.Id }, cancellationToken: cancellationToken)).QueueStats.Queue;
            }

            result.Add(new QueueInfo(
                queue.Id,
                queue.CompartmentId,
                queue.DisplayName,
                OciValues.State(queue.LifecycleState),
                queue.MessagesEndpoint,
                stats?.VisibleMessages,
                stats?.InFlightMessages,
                queue.VisibilityInSeconds,
                queue.RetentionInSeconds,
                queue.TimeoutInSeconds,
                queue.TimeCreated.GetValueOrDefault()));
        }

        result.Sort(static (x, y) => String.Compare(x.DisplayName, y.DisplayName, StringComparison.Ordinal));
        return result;
    }

    // Puts one message
    public async ValueTask PutMessageAsync(string queueId, string messagesEndpoint, string content, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateQueueClient(messagesEndpoint);
        await client.PutMessages(
            new PutMessagesRequest
            {
                QueueId = queueId,
                PutMessagesDetails = new PutMessagesDetails { Messages = [new PutMessagesDetailsEntry { Content = content }] }
            },
            cancellationToken: cancellationToken);
    }

    // Receives messages; they stay hidden for visibilitySeconds unless deleted
    public async ValueTask<List<QueueMessageInfo>> GetMessagesAsync(string queueId, string messagesEndpoint, int limit, int visibilitySeconds, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateQueueClient(messagesEndpoint);
        var response = await client.GetMessages(
            new GetMessagesRequest { QueueId = queueId, Limit = limit, VisibilityInSeconds = visibilitySeconds, TimeoutInSeconds = 0 },
            cancellationToken: cancellationToken);

#pragma warning disable IDE0028
        return (response.GetMessages.Messages ?? [])
            .Select(static x => new QueueMessageInfo(x.Id ?? 0, x.Receipt, x.Content, x.DeliveryCount ?? 0, x.VisibleAfter, x.ExpireAfter))
            .ToList();
#pragma warning restore IDE0028
    }

    public async ValueTask DeleteMessageAsync(string queueId, string messagesEndpoint, string receipt, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateQueueClient(messagesEndpoint);
        await client.DeleteMessage(new DeleteMessageRequest { QueueId = queueId, MessageReceipt = receipt }, cancellationToken: cancellationToken);
    }

    // Purges the queue and its dead letter queue
    public async ValueTask PurgeQueueAsync(string queueId, CancellationToken cancellationToken = default)
    {
        using var admin = factory.CreateQueueAdminClient();
        await admin.PurgeQueue(
            new PurgeQueueRequest { QueueId = queueId, PurgeQueueDetails = new PurgeQueueDetails { PurgeType = PurgeQueueDetails.PurgeTypeEnum.Both } },
            cancellationToken: cancellationToken);
    }
}
#pragma warning restore CA1724
