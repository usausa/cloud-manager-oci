namespace CloudManager.Models.OracleCloud.ObjectStorage;

public sealed record BucketInfo(
    string Name,
    string Namespace,
    string CompartmentId,
    string? CreatedBy,
    DateTime TimeCreated);
