namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class IdentityDomainsPage
{
    private List<IdentityDomainInfo> domains = [];

    private List<DomainUserInfo> users = [];

    private IdentityDomainInfo? selectedDomain;

    private bool isUsersLoading;

    private string userSearchText = string.Empty;

    [Inject]
    public required IdentityDomainsService Service { get; set; }

    private IEnumerable<DomainUserInfo> FilteredUsers =>
        String.IsNullOrWhiteSpace(userSearchText)
            ? users
            : users.Where(x =>
                x.UserName.Contains(userSearchText, StringComparison.OrdinalIgnoreCase) ||
                (x.DisplayName ?? string.Empty).Contains(userSearchText, StringComparison.OrdinalIgnoreCase) ||
                (x.Email ?? string.Empty).Contains(userSearchText, StringComparison.OrdinalIgnoreCase));

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedDomain = null;
        users = [];
        return LoadAsync(async () =>
        {
            domains = await Service.ListDomainsAsync(CancellationToken);
        });
    }

    private Task OnDomainSelectedAsync(IdentityDomainInfo? domain)
    {
        selectedDomain = domain;
        users = [];
        return domain is null ? Task.CompletedTask : LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        if (selectedDomain is null)
        {
            return;
        }

        isUsersLoading = true;
        try
        {
            users = await Service.ListUsersAsync(selectedDomain.Endpoint, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isUsersLoading = false;
        }
    }

    private async Task ResetPasswordAsync(DomainUserInfo user)
    {
        if (selectedDomain is null)
        {
            return;
        }

        if (await DialogService.ShowOperationConfirm("パスワードリセット確認", $"ユーザー「{user.UserName}」にパスワードリセットの通知を送ります。", requireConfirmText: user.UserName) is null)
        {
            return;
        }

        await RunAsync("実行中...", async (_, cancellationToken) =>
        {
            await Service.ResetPasswordAsync(selectedDomain.Endpoint, user.Id, cancellationToken);
            Snackbar.AddSuccess($"パスワードリセットを要求しました: {user.UserName}");
        });
    }

    private async Task SetPasswordAsync(DomainUserInfo user)
    {
        if (selectedDomain is null)
        {
            return;
        }

        var dialog = await DialogService.ShowAsync<SetPasswordDialog>("パスワード設定", new DialogParameters<SetPasswordDialog>
        {
            { x => x.UserName, user.UserName }
        });
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        var password = (string)result.Data!;
        await RunAsync("設定中...", async (_, cancellationToken) =>
        {
            await Service.SetPasswordAsync(selectedDomain.Endpoint, user.Id, password, cancellationToken);
            Snackbar.AddSuccess($"パスワードを設定しました: {user.UserName}");
        });
    }
}
