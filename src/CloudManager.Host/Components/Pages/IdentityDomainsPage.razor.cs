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

    private Task LoadUsersAsync()
    {
        if (selectedDomain is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            users = await Service.ListUsersAsync(selectedDomain.Endpoint, CancellationToken);
        }, x => isUsersLoading = x);
    }

    private async Task ResetPasswordAsync(DomainUserInfo user)
    {
        if (selectedDomain is null)
        {
            return;
        }

        if (await DialogService.ShowOperationConfirm("パスワードリセット", $"ユーザー「{user.UserName}」にパスワードリセットの通知を送りますか？", requireConfirmText: user.UserName) is null)
        {
            return;
        }

        await RunAsync("パスワードリセット中...", async (_, cancellationToken) =>
        {
            await Service.ResetPasswordAsync(selectedDomain.Endpoint, user.Id, cancellationToken);
            Snackbar.AddSuccess($"{user.UserName} のパスワードリセットを要求しました。");
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
            Snackbar.AddSuccess($"{user.UserName} のパスワードを設定しました。");
        });
    }
}
