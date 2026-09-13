namespace CloudManager.Models.OracleCloud.ResourceSearch;

// State is normalized to upper case
public sealed record ResourceStateCount(
    string ResourceType,
    string State,
    int Count);
