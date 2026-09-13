namespace CloudManager.Models.OracleCloud.Bastion;

public sealed record BastionInfo(
    string Id,
    string Name,
    string BastionType,
    string TargetVcnId,
    string TargetSubnetId,
    string State,
    DateTime? TimeCreated);
