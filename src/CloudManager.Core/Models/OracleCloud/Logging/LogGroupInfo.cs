namespace CloudManager.Models.OracleCloud.Logging;

public sealed record LogGroupInfo(
    string Id,
    string DisplayName,
    string? Description,
    string State,
    DateTime? TimeCreated);
