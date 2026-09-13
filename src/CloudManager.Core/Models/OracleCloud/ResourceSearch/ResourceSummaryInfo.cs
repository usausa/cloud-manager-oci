namespace CloudManager.Models.OracleCloud.ResourceSearch;

public sealed record ResourceSummaryInfo(
    string ResourceType,
    string Identifier,
    string? DisplayName,
    string? State,
    string CompartmentId,
    string? AvailabilityDomain,
    DateTime? TimeCreated);
