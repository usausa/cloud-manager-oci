namespace CloudManager.Host.Settings;

public sealed class OciSetting
{
    [Required]
    public string DefaultProfile { get; set; } = default!;

    public string? DefaultRegion { get; set; }

    // Defaults to the tenancy root when not specified
    public string? DefaultCompartmentId { get; set; }
}
