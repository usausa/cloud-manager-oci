namespace CloudManager.Models.OracleCloud.Vault;

public sealed record SecretInfo(
    string Id,
    string CompartmentId,
    string SecretName,
    string VaultId,
    string State,
    string? Description,
    string RotationStatus,
    DateTime? TimeCreated,
    DateTime? TimeOfCurrentVersionExpiry,
    DateTime? LastRotationTime);
