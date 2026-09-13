namespace CloudManager.Models.OracleCloud.Dns;

public sealed record DnsZoneInfo(
    string Id,
    string CompartmentId,
    string Name,
    string ZoneType,
    string Scope,
    string State,
    long? Serial,
    DateTime? TimeCreated);
