namespace CloudManager.Models.OracleCloud.Notifications;

public sealed record SubscriptionInfo(
    string Id,
    string Protocol,
    string Endpoint,
    string State,
    DateTime? CreatedTime);
