namespace CloudManager.Host.Infrastructure.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Identity;
using CloudManager.Services.OracleCloud;

using Oci.Common;

// Holds the current profile, region and compartment per circuit
public sealed class OciSession
{
    private OciContext? context;

    private Task? loading;

    public string ProfileName { get; private set; }

    public Region? Region { get; private set; }

    public string? TenancyId { get; private set; }

    public string CompartmentId { get; private set; }

    // The compartment path once loaded, otherwise the id
    public string CompartmentName { get; private set; }

    // Compartments listed for the selection: itself and its descendants once loaded (the root covers the tenancy)
    public IReadOnlyList<string> ScopeCompartmentIds { get; private set; }

    public IReadOnlyList<string> AvailableProfiles { get; }

    public IReadOnlyList<CompartmentInfo> Compartments { get; private set; } = [];

    public IReadOnlyList<string> AvailableRegions { get; private set; } = [];

    // Whether the current profile exists in ~/.oci/config; OCI calls fail without it
    public bool IsProfileAvailable => AvailableProfiles.Contains(ProfileName, StringComparer.Ordinal);

    public bool IsLoaded { get; private set; }

    // Raised when the profile, region or compartment changes so that pages can reload
    public event EventHandler? Changed;

    public OciSession(ILogger<OciSession> log, OciSetting setting)
    {
        ProfileName = setting.DefaultProfile;
        AvailableProfiles = LoadProfiles(log);

        // The region and tenancy are read from the config file without touching the private key
        if (!String.IsNullOrWhiteSpace(setting.DefaultRegion))
        {
            Region = Region.FromRegionId(setting.DefaultRegion);
        }
        else if (IsProfileAvailable)
        {
            var regionId = ProfileResolver.GetProfileValue(ProfileName, "region");
            Region = String.IsNullOrWhiteSpace(regionId) ? null : Region.FromRegionId(regionId);
        }

        TenancyId = IsProfileAvailable ? ProfileResolver.GetProfileValue(ProfileName, "tenancy") : null;
        CompartmentId = String.IsNullOrWhiteSpace(setting.DefaultCompartmentId) ? TenancyId ?? string.Empty : setting.DefaultCompartmentId;
        CompartmentName = CompartmentId;
        ScopeCompartmentIds = [CompartmentId];
    }

    // Resolved lazily so that a broken profile only fails when a call is made
    public OciContext ResolveContext() =>
        context ??= ProfileResolver.Resolve(ProfileName, Region?.RegionId, CompartmentId) with { CompartmentIds = ScopeCompartmentIds };

    // Switches the profile and region, throwing when they cannot be resolved
    public void SetProfile(string profileName, string? regionId)
    {
        var resolved = ProfileResolver.Resolve(profileName, regionId, null);
        ProfileName = profileName;
        Region = resolved.Region;
        TenancyId = resolved.TenancyId;
        CompartmentId = resolved.CompartmentId;
        CompartmentName = CompartmentId;
        ScopeCompartmentIds = [CompartmentId];
        Compartments = [];
        AvailableRegions = [];
        IsLoaded = false;
        loading = null;
        context = resolved;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetCompartment(string compartmentId)
    {
        if (String.Equals(compartmentId, CompartmentId, StringComparison.Ordinal))
        {
            return;
        }

        CompartmentId = compartmentId;
        UpdateScope();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    // Loads the compartments and subscribed regions once per profile; concurrent callers share the load
    public Task EnsureLoadedAsync(IdentityService identity)
    {
        if (IsLoaded || !IsProfileAvailable)
        {
            return Task.CompletedTask;
        }

        return loading ??= LoadAsync(identity);
    }

    private async Task LoadAsync(IdentityService identity)
    {
        try
        {
            var compartments = await identity.ListCompartmentsAsync();
            var regions = await identity.ListRegionSubscriptionsAsync();

            Compartments = compartments;
#pragma warning disable IDE0028
            AvailableRegions = regions.Select(static x => x.RegionName).ToList();
#pragma warning restore IDE0028
            UpdateScope();
            IsLoaded = true;
        }
        finally
        {
            loading = null;
        }
    }

    // The name and scope follow the loaded compartment tree; the context is rebuilt on the next call
    private void UpdateScope()
    {
        CompartmentName = Compartments.FirstOrDefault(x => x.Id == CompartmentId)?.Path ?? CompartmentId;
        ScopeCompartmentIds = CompartmentScope.Subtree(Compartments, CompartmentId);
        context = null;
    }

    private static IReadOnlyList<string> LoadProfiles(ILogger<OciSession> log)
    {
        try
        {
            return ProfileResolver.ListProfiles();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Keep the UI working even if the config file is missing or broken
            log.WarnOciProfileLoadFailed(ex);
            return [];
        }
    }
}
