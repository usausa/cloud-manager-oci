namespace CloudManager.Models.OracleCloud.Vault;

public sealed record SecretValueInfo(
    string SecretName,
    string Content,
    long? VersionNumber,
    string? VersionName,
    DateTime? TimeCreated);
