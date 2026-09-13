namespace CloudManager.Models.OracleCloud.ContainerRegistry;

// Address is the pull path without a tag ({region}.ocir.io/{namespace}/{name})
public sealed record ContainerRepositoryInfo(
    string Id,
    string DisplayName,
    string Address,
    string State,
    int ImageCount,
    long LayersSizeBytes,
    bool IsPublic,
    DateTime TimeCreated);
