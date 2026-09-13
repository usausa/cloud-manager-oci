namespace CloudManager.Models.OracleCloud.Certificates;

public sealed record CertificateDetail(
    string Id,
    string Name,
    string? Description,
    string? CommonName,
    IReadOnlyList<string> SubjectAlternativeNames,
    string ConfigType,
    string? IssuerCaId,
    string KeyAlgorithm,
    string SignatureAlgorithm,
    long? VersionNumber,
    string? SerialNumber,
    DateTime? NotBefore,
    DateTime? NotAfter,
    IReadOnlyList<string> Stages);
