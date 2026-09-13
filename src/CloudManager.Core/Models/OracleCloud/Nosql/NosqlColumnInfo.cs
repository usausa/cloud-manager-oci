namespace CloudManager.Models.OracleCloud.Nosql;

public sealed record NosqlColumnInfo(
    string Name,
    string Type,
    bool IsNullable);
