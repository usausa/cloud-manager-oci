namespace CloudManager.Models.OracleCloud.AutonomousDatabase;

public sealed record AutonomousDatabaseBackupInfo(
    string Id,
    string DisplayName,
    string DatabaseId,
    string State,
    string Type,
    bool IsAutomatic,
    bool IsRestorable,
    DateTime? TimeStarted,
    DateTime? TimeEnded,
    double? SizeTb,
    int? RetentionDays);
