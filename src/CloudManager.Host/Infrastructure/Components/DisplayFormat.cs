namespace CloudManager.Host.Infrastructure.Components;

// Formatting helpers shared by pages and dialogs
public static class DisplayFormat
{
    // OCIDs are long, so show the resource type and the tail (ocid1.instance…abcdef)
    public static string Ocid(string? id)
    {
        if (String.IsNullOrEmpty(id) || (id.Length <= 24))
        {
            return id ?? "-";
        }

        var second = id.IndexOf('.', id.IndexOf('.', StringComparison.Ordinal) + 1);
        var prefix = second > 0 ? id[..second] : id[..12];
        return $"{prefix}…{id[^8..]}";
    }

    public static string Size(long bytes) => bytes switch
    {
        >= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1} GB",
        >= 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        >= 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes} B"
    };

    public static string Time(DateTime? time) =>
        time.HasValue ? time.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "-";

    public static string Number(decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);
}
