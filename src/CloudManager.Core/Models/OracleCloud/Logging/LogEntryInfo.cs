namespace CloudManager.Models.OracleCloud.Logging;

// Raw is the full JSON of the log entry
public sealed record LogEntryInfo(
    DateTime? Timestamp,
    string Message,
    string Raw);
