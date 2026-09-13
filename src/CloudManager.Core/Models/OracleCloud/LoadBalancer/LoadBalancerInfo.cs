namespace CloudManager.Models.OracleCloud.LoadBalancer;

// Type is LB (application load balancer) or NLB (network load balancer)
public sealed record LoadBalancerInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string Type,
    string State,
    string? ShapeName,
    bool IsPrivate,
    string IpAddresses,
    int BackendSetCount,
    int ListenerCount,
    DateTime TimeCreated);
