namespace CloudManager.Models.OracleCloud.Nosql;

// Values are shown as strings, nested values as JSON
public sealed record NosqlRowInfo(
    Dictionary<string, string> Columns);
