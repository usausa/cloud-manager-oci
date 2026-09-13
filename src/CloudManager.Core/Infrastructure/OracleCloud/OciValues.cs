namespace CloudManager.Infrastructure.OracleCloud;

using Oci.Common.Utils;

// Conversions from SDK values to the strings shown in the UI
public static class OciValues
{
    // Enum values are shown as their REST names (RUNNING, STOPPED, ...)
    public static string State<T>(T? value)
        where T : struct, Enum =>
        value.HasValue ? HttpUtils.GetEnumString(value.Value) : "-";

    public static string State<T>(T value)
        where T : struct, Enum =>
        HttpUtils.GetEnumString(value);

    // Resource Search returns mixed-case states, so normalize before comparing
    public static bool IsState(string? value, string state) =>
        String.Equals(value, state, StringComparison.OrdinalIgnoreCase);

    // Maps a REST name (RUNNING) back to the SDK enum, null when empty or unknown
    public static T? ParseState<T>(string? value)
        where T : struct, Enum
    {
        if (String.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var candidate in Enum.GetValues<T>())
        {
            if (String.Equals(HttpUtils.GetEnumString(candidate), value, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    // Tags are shown as key=value pairs
    public static string Tags(Dictionary<string, string>? tags) =>
        tags is null || tags.Count == 0 ? string.Empty : String.Join(", ", tags.Select(static x => $"{x.Key}={x.Value}"));
}
