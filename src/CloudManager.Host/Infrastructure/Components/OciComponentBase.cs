namespace CloudManager.Host.Infrastructure.Components;

using CloudManager.Host.Infrastructure.OracleCloud;
using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Services.OracleCloud;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// Base class for OCI pages, centralizing loading, running and error state
public abstract class OciComponentBase : AppComponentBase
{
    private CancellationTokenSource? cancellation;

    [Inject]
    public required OciSession Session { get; set; }

    [Inject]
    public required IdentityService IdentityService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    protected bool IsLoading { get; private set; }

    protected bool IsRunning { get; private set; }

    // Reload buttons are disabled while loading or running
    protected bool IsBusy => IsLoading || IsRunning;

    protected double ProgressRatio { get; private set; }

    protected string? ProgressMessage { get; private set; }

    protected string? ErrorMessage { get; set; }

    // Cancel running operations when the circuit is disposed
    protected CancellationToken CancellationToken => (cancellation ??= new CancellationTokenSource()).Token;

    protected override void OnInitialized()
    {
        Session.Changed += OnSessionChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Session.Changed -= OnSessionChanged;

            if (cancellation is not null)
            {
                cancellation.Cancel();
                cancellation.Dispose();
                cancellation = null;
            }
        }

        base.Dispose(disposing);
    }

    // Pages override this to reload when the profile or compartment changes
    protected virtual Task OnSessionChangedAsync() => Task.CompletedTask;

    // Lists spanning several compartments show where each row lives
    protected bool ShowCompartment => Session.ScopeCompartmentIds.Count > 1;

    protected string CompartmentName(string? compartmentId) =>
        Session.Compartments.FirstOrDefault(x => x.Id == compartmentId)?.Name ?? DisplayFormat.Ocid(compartmentId);

    protected string CompartmentPath(string? compartmentId) =>
        Session.Compartments.FirstOrDefault(x => x.Id == compartmentId)?.Path ?? compartmentId ?? "-";

    // Loads data, showing failures in the banner
    protected Task LoadAsync(Func<Task> load) =>
        LoadAsync(load, x => IsLoading = x);

    // Loads a detail list that has its own loading flag
    protected async Task LoadAsync(Func<Task> load, Action<bool> setLoading)
    {
        setLoading(true);
        ErrorMessage = null;
        try
        {
            await EnsureScopeAsync();
            await load();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            setLoading(false);
        }
    }

    // Runs an operation with a progress overlay and reports failures
    protected async Task RunAsync(string message, Func<IProgress<ProgressUpdate>, CancellationToken, Task> operation, Func<Task>? reload = null)
    {
        IsRunning = true;
        ProgressRatio = 0;
        ProgressMessage = message;
        ErrorMessage = null;

        var progress = new Progress<ProgressUpdate>(x =>
        {
            ProgressRatio = x.Ratio;
            ProgressMessage = x.Message;
            _ = InvokeAsync(StateHasChanged);
        });

        try
        {
            await operation(progress, CancellationToken);
            if (reload is not null)
            {
                await reload();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
            Snackbar.AddError(ErrorMessage);
        }
        finally
        {
            IsRunning = false;
            ProgressRatio = 0;
            ProgressMessage = null;
        }
    }

    protected static string FormatError(Exception ex) => ex.FormatError();

    // The compartment tree decides which compartments a listing covers, so it is loaded before any listing;
    // a failure is reported by the compartment selector and the listing falls back to the selected compartment
    private async Task EnsureScopeAsync()
    {
        try
        {
            await Session.EnsureLoadedAsync(IdentityService);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Reported by the compartment selector
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(async () =>
        {
            await OnSessionChangedAsync();
            StateHasChanged();
        });
    }
}
