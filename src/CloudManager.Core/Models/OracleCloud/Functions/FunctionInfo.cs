namespace CloudManager.Models.OracleCloud.Functions;

public sealed record FunctionInfo(
    string Id,
    string DisplayName,
    string ApplicationId,
    string State,
    string? Image,
    int? MemoryMb,
    int? TimeoutSeconds,
    string InvokeEndpoint,
    int? ProvisionedConcurrency,
    DateTime? TimeUpdated);
