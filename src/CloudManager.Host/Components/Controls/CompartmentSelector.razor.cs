namespace CloudManager.Host.Components.Controls;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

// Compartment switch shown in the app bar; pages reload through the session change event
public sealed partial class CompartmentSelector
{
    [Inject]
    public required IdentityService IdentityService { get; set; }

    protected override Task OnInitializedAsync() => LoadCompartmentsAsync();

    // The list is reloaded after a profile switch
    protected override Task OnSessionChangedAsync() => LoadCompartmentsAsync();

    private async Task LoadCompartmentsAsync()
    {
        if (!Session.IsProfileAvailable || Session.IsLoaded)
        {
            return;
        }

        await LoadAsync(() => Session.EnsureLoadedAsync(IdentityService));
        if (ErrorMessage is not null)
        {
            Snackbar.AddWarning($"コンパートメントの取得に失敗しました: {ErrorMessage}");
        }
    }

    private void OnCompartmentChanged(string compartmentId) => Session.SetCompartment(compartmentId);
}
