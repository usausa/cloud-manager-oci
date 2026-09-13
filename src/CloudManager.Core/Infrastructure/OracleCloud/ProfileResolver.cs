namespace CloudManager.Infrastructure.OracleCloud;

using Oci.Common;
using Oci.Common.Auth;

// Resolves a profile from ~/.oci/config into an authentication provider, region and compartment
public static class ProfileResolver
{
    public const string DefaultProfileName = "DEFAULT";

    // Profile names defined in ~/.oci/config
    public static IReadOnlyList<string> ListProfiles() =>
        [.. ConfigFileReader.ParseDefault().GetConfiguration().Keys];

    // Reads a single value of the profile without touching the private key
    public static string? GetProfileValue(string profileName, string key)
    {
        try
        {
            return ConfigFileReader.ParseDefault(profileName).GetValue(key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    // The compartment defaults to the tenancy root when not specified
    public static OciContext Resolve(string? profileName, string? regionId, string? compartmentId)
    {
        var name = String.IsNullOrWhiteSpace(profileName) ? DefaultProfileName : profileName;

        if (!ProfileExists(name))
        {
            throw new InvalidOperationException($"OCI profile '{name}' not found in ~/.oci/config.");
        }

        var provider = new ConfigFileAuthenticationDetailsProvider(name);

        Region region;
        if (!String.IsNullOrWhiteSpace(regionId))
        {
            region = Region.FromRegionId(regionId);
        }
        else
        {
            region = provider.Region ??
                     throw new InvalidOperationException($"Region not specified. Select a region or set region in ~/.oci/config for profile '{name}'.");
        }

        var tenancyId = provider.TenantId;
        var selected = String.IsNullOrWhiteSpace(compartmentId) ? tenancyId : compartmentId;
        return new OciContext(provider, region, tenancyId, selected, [selected]);
    }

    private static bool ProfileExists(string name)
    {
        try
        {
            return ConfigFileReader.ParseDefault().GetConfiguration().ContainsKey(name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A missing or broken config file means no profile is available
            return false;
        }
    }
}
