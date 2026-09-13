namespace CloudManager.Models.OracleCloud.Bastion;

public sealed record BastionSessionInfo(
    string Id,
    string DisplayName,
    string SessionType,
    string? TargetResourceId,
    string? TargetPrivateIp,
    int? TargetPort,
    string State,
    int? TtlSeconds,
    DateTime? TimeCreated);
