namespace CloudManager.Models.OracleCloud.ObjectStorage;

// One level of the object hierarchy: sub-prefixes and the objects directly under the prefix
public sealed record ObjectListing(
    IReadOnlyList<string> Prefixes,
    IReadOnlyList<ObjectInfo> Objects);
