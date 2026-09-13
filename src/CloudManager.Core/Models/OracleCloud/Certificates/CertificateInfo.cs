namespace CloudManager.Models.OracleCloud.Certificates;

public sealed record CertificateInfo(
    string Id,
    string CompartmentId,
    string Name,
    string State,
    string ConfigType,
    string? CommonName,
    DateTime? NotAfter,
    int? DaysToExpiry,
    DateTime? TimeCreated);
