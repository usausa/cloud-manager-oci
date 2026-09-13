namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class VaultPage
{
    private List<SecretInfo> secrets = [];

    private string searchText = string.Empty;

    [Inject]
    public required VaultService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            secrets = await Service.ListSecretsAsync(CancellationToken);
        });

    private bool FilterFunc(SecretInfo secret) =>
        String.IsNullOrWhiteSpace(searchText) || secret.SecretName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private async Task ShowValueAsync(SecretInfo secret)
    {
        SecretValueInfo? value = null;
        await RunAsync("取得中...", async (_, cancellationToken) =>
        {
            value = await Service.GetSecretValueAsync(secret.Id, secret.SecretName, cancellationToken);
        });
        if (value is null)
        {
            return;
        }

        await DialogService.ShowAsync<SecretValueDialog>("シークレット値", new DialogParameters<SecretValueDialog>
        {
            { x => x.Value, value }
        },
        Styles.MediumDialog);
    }

    private async Task RotateAsync(SecretInfo secret)
    {
        if (await DialogService.ShowOperationConfirm("ローテーション", $"シークレット「{secret.SecretName}」を即時ローテーションします。ターゲットシステムの設定が必要です。") is null)
        {
            return;
        }

        await RunAsync("ローテーション中...", async (_, cancellationToken) =>
        {
            await Service.RotateSecretAsync(secret.Id, cancellationToken);
            Snackbar.AddSuccess($"{secret.SecretName} のローテーションを開始しました。");
            await LoadAsync();
        });
    }
}
