namespace CloudManager.Models.OracleCloud.ApiGateway;

public sealed record ApiGatewayInfo(
    string Id,
    string DisplayName,
    string EndpointType,
    string? Hostname,
    string State,
    DateTime TimeCreated);
