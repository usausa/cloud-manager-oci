namespace CloudManager.Models.OracleCloud.ObjectStorage;

public sealed record ObjectInfo(
    string Name,
    long Size,
    DateTime? TimeModified,
    string StorageTier);
