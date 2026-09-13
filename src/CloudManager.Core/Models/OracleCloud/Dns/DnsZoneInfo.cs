namespace CloudManager.Models.OracleCloud.Dns;

public sealed record DnsZoneInfo(
    string Id,
    string Name,
    string ZoneType,
    string Scope,
    string State,
    long? Serial,
    DateTime? TimeCreated);
