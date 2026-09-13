namespace CloudManager.Models.OracleCloud.ObjectStorage;

public sealed record ObjectVersionInfo(
    string VersionId,
    bool IsLatest,
    DateTime? TimeModified,
    long Size,
    bool IsDeleteMarker);
