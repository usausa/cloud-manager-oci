namespace CloudManager.Models.OracleCloud.Identity;

public sealed record RegionSubscriptionInfo(
    string RegionName,
    string RegionKey,
    bool IsHomeRegion,
    string Status);
