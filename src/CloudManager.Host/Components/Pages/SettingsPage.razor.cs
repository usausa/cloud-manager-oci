namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class SettingsPage
{
    private string selectedProfile = string.Empty;

    private string selectedRegion = string.Empty;

    private string selectedCompartment = string.Empty;

    [Inject]
    public required IdentityService IdentityService { get; set; }

    // Subscribed regions once loaded, otherwise the current region only
    private IReadOnlyList<string> Regions =>
        Session.AvailableRegions.Count > 0
            ? Session.AvailableRegions
            : String.IsNullOrEmpty(selectedRegion) ? [] : [selectedRegion];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SyncSelection();
    }

    protected override Task OnInitializedAsync() => LoadCompartmentsAsync();

    protected override Task OnSessionChangedAsync()
    {
        SyncSelection();
        return LoadCompartmentsAsync();
    }

    private void SyncSelection()
    {
        selectedProfile = Session.ProfileName;
        selectedRegion = Session.Region?.RegionId ?? string.Empty;
        selectedCompartment = Session.CompartmentId;
    }

    private Task LoadCompartmentsAsync() =>
        Session.IsProfileAvailable ? LoadAsync(() => Session.EnsureLoadedAsync(IdentityService)) : Task.CompletedTask;

    private void Apply()
    {
        ErrorMessage = null;
        try
        {
            var profileChanged = !String.Equals(selectedProfile, Session.ProfileName, StringComparison.Ordinal);
            var regionChanged = !String.Equals(selectedRegion, Session.Region?.RegionId, StringComparison.Ordinal);
            if (profileChanged || regionChanged)
            {
                // Switching the profile resets the compartment to the tenancy root
                Session.SetProfile(selectedProfile, selectedRegion);
            }
            else if (!String.IsNullOrEmpty(selectedCompartment))
            {
                Session.SetCompartment(selectedCompartment);
            }

            Snackbar.AddSuccess("設定を適用しました。");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
    }
}
