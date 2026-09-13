namespace CloudManager.Models.OracleCloud.Monitoring;

// Status is FIRING / OK / SUSPENDED from the alarm status API
public sealed record AlarmInfo(
    string Id,
    string DisplayName,
    string Namespace,
    string Query,
    string Severity,
    bool IsEnabled,
    string State,
    string Status,
    DateTime? TimestampTriggered);
