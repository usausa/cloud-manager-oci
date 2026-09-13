namespace CloudManager.Models.OracleCloud.LoadBalancer;

public sealed record BackendSetInfo(
    string Name,
    string Policy,
    int BackendCount,
    string HealthStatus,
    string? HealthCheckProtocol,
    int? HealthCheckPort,
    string? HealthCheckPath);
