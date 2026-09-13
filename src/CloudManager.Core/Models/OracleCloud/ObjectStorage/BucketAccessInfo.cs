namespace CloudManager.Models.OracleCloud.ObjectStorage;

// Visibility and settings of a bucket
public sealed record BucketAccessInfo(
    string Name,
    string PublicAccessType,
    string Versioning,
    string StorageTier,
    bool AutoTiering,
    long? ApproximateCount,
    long? ApproximateSize,
    string Verdict);
