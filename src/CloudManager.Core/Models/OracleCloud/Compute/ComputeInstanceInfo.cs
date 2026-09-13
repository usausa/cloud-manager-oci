namespace CloudManager.Models.OracleCloud.Compute;

public sealed record ComputeInstanceInfo(
    string Id,
    string DisplayName,
    string State,
    string Shape,
    float? Ocpus,
    float? MemoryGb,
    string? PublicIp,
    string? PrivateIp,
    string AvailabilityDomain,
    string Tags,
    DateTime TimeCreated);
