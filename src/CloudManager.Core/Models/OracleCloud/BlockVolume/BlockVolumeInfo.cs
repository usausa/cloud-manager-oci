namespace CloudManager.Models.OracleCloud.BlockVolume;

// Boot volumes and block volumes are listed together, distinguished by IsBootVolume
public sealed record BlockVolumeInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string State,
    long SizeGb,
    long? VpusPerGb,
    string AvailabilityDomain,
    bool IsBootVolume,
    string? AttachedInstanceId,
    string? AttachmentId,
    DateTime TimeCreated);
