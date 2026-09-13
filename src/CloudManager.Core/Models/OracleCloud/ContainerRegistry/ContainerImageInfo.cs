namespace CloudManager.Models.OracleCloud.ContainerRegistry;

public sealed record ContainerImageInfo(
    string Id,
    string DisplayName,
    string? Version,
    string Digest,
    string State,
    DateTime TimeCreated);
