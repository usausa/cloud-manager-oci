namespace CloudManager.Models.OracleCloud.ApiGateway;

public sealed record ApiGatewayInfo(
    string Id,
    string CompartmentId,
    string DisplayName,
    string EndpointType,
    string? Hostname,
    string State,
    DateTime TimeCreated);
