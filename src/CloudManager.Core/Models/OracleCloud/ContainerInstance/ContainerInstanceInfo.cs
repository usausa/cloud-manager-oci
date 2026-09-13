namespace CloudManager.Models.OracleCloud.ContainerInstance;

public sealed record ContainerInstanceInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string State,
    string Shape,
    float? Ocpus,
    float? MemoryGb,
    int ContainerCount,
    string AvailabilityDomain,
    DateTime TimeCreated);
