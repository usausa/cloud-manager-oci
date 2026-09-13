namespace CloudManager.Models.OracleCloud.AutonomousDatabase;

// Asynchronous operation history of a resource
public sealed record WorkRequestInfo(
    string Id,
    string OperationType,
    string Status,
    float? PercentComplete,
    DateTime? TimeAccepted,
    DateTime? TimeStarted,
    DateTime? TimeFinished);
