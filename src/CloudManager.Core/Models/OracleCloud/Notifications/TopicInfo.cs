namespace CloudManager.Models.OracleCloud.Notifications;

public sealed record TopicInfo(
    string TopicId,
    string Name,
    string State,
    string? Description,
    int SubscriptionCount,
    DateTime? TimeCreated);
