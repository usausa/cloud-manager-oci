namespace CloudManager.Models.OracleCloud.ApiGateway;

public sealed record ApiDeploymentInfo(
    string Id,
    string DisplayName,
    string PathPrefix,
    string? Endpoint,
    string State,
    DateTime? TimeUpdated);
