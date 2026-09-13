namespace CloudManager.Models.OracleCloud.Compute;

// Output of a Run Command executed through the compute instance agent
public sealed record RunCommandResult(
    string Status,
    string? Output,
    int? ExitCode,
    string? Message);
