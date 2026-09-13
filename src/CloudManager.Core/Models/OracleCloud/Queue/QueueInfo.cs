namespace CloudManager.Models.OracleCloud.Queue;

// MessagesEndpoint is the data plane host used for put/get
public sealed record QueueInfo(
    string Id,
    string DisplayName,
    string State,
    string MessagesEndpoint,
    long? VisibleMessages,
    long? InFlightMessages,
    int? VisibilityInSeconds,
    int? RetentionInSeconds,
    int? TimeoutInSeconds,
    DateTime TimeCreated);
