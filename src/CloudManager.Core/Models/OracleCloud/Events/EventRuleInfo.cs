namespace CloudManager.Models.OracleCloud.Events;

public sealed record EventRuleInfo(
    string Id,
    string DisplayName,
    string? Description,
    bool IsEnabled,
    string State,
    string Condition,
    IReadOnlyList<string> Actions,
    DateTime? TimeCreated);
