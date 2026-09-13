namespace CloudManager.Models.OracleCloud.Nosql;

public sealed record NosqlTableDetail(
    string Name,
    string? Ddl,
    int? TtlDays,
    IReadOnlyList<NosqlColumnInfo> Columns,
    IReadOnlyList<string> PrimaryKey);
