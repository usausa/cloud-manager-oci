namespace CloudManager.Models.OracleCloud.Functions;

// Function configuration is exposed to the container as environment variables
public sealed record FunctionsConfigEntry(
    string Key,
    string Value);
