namespace CloudManager.Models.OracleCloud.Nosql;

public sealed record NosqlTableInfo(
    string Id,
    string CompartmentId,
    string Name,
    string State,
    string CapacityMode,
    int? MaxReadUnits,
    int? MaxWriteUnits,
    int? MaxStorageGb,
    DateTime TimeCreated);
