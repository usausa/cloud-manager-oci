namespace CloudManager.Host.Components.Controls;

using CloudManager.Host.Infrastructure.Components;

// Compartment switch shown in the app bar; pages reload through the session change event
public sealed partial class CompartmentSelector
{
    protected override Task OnInitializedAsync() => LoadCompartmentsAsync();

    // The list is reloaded after a profile switch
    protected override Task OnSessionChangedAsync() => LoadCompartmentsAsync();

    private async Task LoadCompartmentsAsync()
    {
        if (!Session.IsProfileAvailable || Session.IsLoaded)
        {
            return;
        }

        try
        {
            await Session.EnsureLoadedAsync(IdentityService);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Snackbar.AddWarning($"コンパートメントの取得に失敗しました: {FormatError(ex)}");
        }
    }

    private void OnCompartmentChanged(string compartmentId) => Session.SetCompartment(compartmentId);
}
