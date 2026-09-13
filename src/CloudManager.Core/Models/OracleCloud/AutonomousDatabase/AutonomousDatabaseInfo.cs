namespace CloudManager.Models.OracleCloud.AutonomousDatabase;

public sealed record AutonomousDatabaseInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string DbName,
    string State,
    string Workload,
    string? DbVersion,
    float? ComputeCount,
    string ComputeModel,
    int? StorageGb,
    bool IsFreeTier,
    bool IsAutoScalingEnabled,
    string LicenseModel,
    DateTime TimeCreated);
