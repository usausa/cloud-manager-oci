namespace CloudManager.Models.OracleCloud.Nosql;

public sealed record NosqlTableInfo(
    string Id,
    string Name,
    string State,
    string CapacityMode,
    int? MaxReadUnits,
    int? MaxWriteUnits,
    int? MaxStorageGb,
    DateTime TimeCreated);
