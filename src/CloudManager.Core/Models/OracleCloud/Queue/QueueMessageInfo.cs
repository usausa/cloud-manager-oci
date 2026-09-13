namespace CloudManager.Models.OracleCloud.Queue;

public sealed record QueueMessageInfo(
    long Id,
    string Receipt,
    string Content,
    int DeliveryCount,
    DateTime? VisibleAfter,
    DateTime? ExpireAfter);
