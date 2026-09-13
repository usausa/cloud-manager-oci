namespace CloudManager.Models.OracleCloud.Functions;

// Detached invocations return an empty payload
public sealed record FunctionsInvokeResult(
    string Payload,
    string? RequestId);
