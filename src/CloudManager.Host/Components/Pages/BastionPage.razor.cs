namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class BastionPage
{
    private const int CreateTimeoutSeconds = 300;

    private List<BastionInfo> bastions = [];

    private List<BastionSessionInfo> sessions = [];

    private BastionInfo? selectedBastion;

    private bool isSessionsLoading;

    [Inject]
    public required BastionService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedBastion = null;
        sessions = [];
        return LoadAsync(async () =>
        {
            bastions = await Service.ListBastionsAsync(CancellationToken);
        });
    }

    private Task OnBastionSelectedAsync(BastionInfo? bastion)
    {
        selectedBastion = bastion;
        sessions = [];
        return bastion is null ? Task.CompletedTask : LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        if (selectedBastion is null)
        {
            return;
        }

        isSessionsLoading = true;
        try
        {
            sessions = await Service.ListSessionsAsync(selectedBastion.Id, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isSessionsLoading = false;
        }
    }

    private static Color SessionStateColor(string state) => state switch
    {
        "ACTIVE" => Color.Success,
        "CREATING" => Color.Info,
        "FAILED" => Color.Error,
        _ => Color.Default
    };

    private async Task ShowSessionAsync(BastionSessionInfo session)
    {
        BastionSessionDetail? detail = null;
        await RunAsync("取得中...", async (_, cancellationToken) =>
        {
            detail = await Service.GetSessionAsync(session.Id, cancellationToken);
        });
        if (detail is null)
        {
            return;
        }

        await ShowDetailAsync(detail);
    }

    // The session is created and awaited until ACTIVE, then the SSH command is shown
    private async Task CreateSessionAsync()
    {
        if (selectedBastion is null)
        {
            return;
        }

        var dialog = await DialogService.ShowAsync<BastionSessionCreateDialog>("セッション作成", new DialogParameters<BastionSessionCreateDialog>
        {
            { x => x.BastionName, selectedBastion.Name }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var p = (BastionSessionCreateParams)result.Data!;
        BastionSessionDetail? detail = null;
        await RunAsync("セッション作成中...", async (progress, cancellationToken) =>
        {
            detail = await Service.CreateSessionAsync(
                selectedBastion.Id,
                p.DisplayName,
                p.SessionType,
                p.TargetResourceId,
                p.TargetPrivateIp,
                p.TargetPort,
                p.OsUserName,
                p.PublicKey,
                p.TtlSeconds,
                CreateTimeoutSeconds,
                progress,
                cancellationToken);
            Snackbar.AddSuccess($"セッション作成完了: {p.DisplayName}");
            await LoadSessionsAsync();
        });
        if (detail is not null)
        {
            await ShowDetailAsync(detail);
        }
    }

    private async Task DeleteSessionAsync(BastionSessionInfo session)
    {
        if (await DialogService.ShowOperationConfirm("セッション削除確認", $"セッション「{session.DisplayName}」を削除します。") is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteSessionAsync(session.Id, cancellationToken);
            Snackbar.AddSuccess($"セッション削除開始: {session.DisplayName}");
            await LoadSessionsAsync();
        });
    }

    private async Task ShowDetailAsync(BastionSessionDetail detail)
    {
        await DialogService.ShowAsync<BastionSessionDialog>("セッション", new DialogParameters<BastionSessionDialog>
        {
            { x => x.Detail, detail }
        });
    }
}
