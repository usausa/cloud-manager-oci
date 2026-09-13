namespace CloudManager.Models.OracleCloud.Vault;

public sealed record SecretInfo(
    string Id,
    string SecretName,
    string VaultId,
    string State,
    string? Description,
    string RotationStatus,
    DateTime? TimeCreated,
    DateTime? TimeOfCurrentVersionExpiry,
    DateTime? LastRotationTime);
