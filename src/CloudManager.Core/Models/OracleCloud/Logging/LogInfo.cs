namespace CloudManager.Models.OracleCloud.Logging;

public sealed record LogInfo(
    string Id,
    string DisplayName,
    string LogType,
    bool IsEnabled,
    string State,
    int? RetentionDays,
    string? Source);
