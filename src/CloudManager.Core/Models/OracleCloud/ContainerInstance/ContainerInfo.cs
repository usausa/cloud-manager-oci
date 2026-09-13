namespace CloudManager.Models.OracleCloud.ContainerInstance;

public sealed record ContainerInfo(
    string Id,
    string DisplayName,
    string State,
    string? Image,
    DateTime TimeCreated);
