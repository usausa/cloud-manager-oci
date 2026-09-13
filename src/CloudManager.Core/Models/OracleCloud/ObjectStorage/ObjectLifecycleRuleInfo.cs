namespace CloudManager.Models.OracleCloud.ObjectStorage;

public sealed record ObjectLifecycleRuleInfo(
    string Name,
    string Action,
    string Target,
    string Period,
    bool IsEnabled,
    IReadOnlyList<string> Prefixes);
