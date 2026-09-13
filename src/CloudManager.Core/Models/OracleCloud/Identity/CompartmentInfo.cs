namespace CloudManager.Models.OracleCloud.Identity;

// Path is the names from the tenancy root joined by " / "
public sealed record CompartmentInfo(
    string Id,
    string Name,
    string Path,
    string? ParentId,
    int Depth);
