namespace CloudManager.Host.Infrastructure.Components;

using CloudManager.Host.Infrastructure.OracleCloud;
using CloudManager.Infrastructure.OracleCloud;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// Base class for OCI pages, centralizing loading, running and error state
public abstract class OciComponentBase : AppComponentBase
{
    private CancellationTokenSource? cancellation;

    [Inject]
    public required OciSession Session { get; set; }

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

    // Loads data, showing failures in the banner
    protected async Task LoadAsync(Func<Task> load)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await load();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            IsLoading = false;
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

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(async () =>
        {
            await OnSessionChangedAsync();
            StateHasChanged();
        });
    }
}
