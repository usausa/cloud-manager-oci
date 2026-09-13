namespace CloudManager.Models.OracleCloud.Functions;

public sealed record FunctionsApplicationInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string State,
    string Shape,
    DateTime TimeCreated);
