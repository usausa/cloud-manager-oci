namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class ContainerImagesPage
{
    private List<ContainerImageInfo> images = [];

    [Inject]
    public required ContainerRegistryService Service { get; set; }

    [Parameter]
    public string RepositoryName { get; set; } = string.Empty;

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            images = await Service.ListImagesAsync(RepositoryName, CancellationToken);
        });

    private async Task DeleteAsync(ContainerImageInfo image)
    {
        var name = image.Version ?? image.Digest;
        var shortDigest = image.Digest.Length > 19 ? image.Digest[^7..] : image.Digest;
        var message = $"イメージ「{name}」を削除します。確認のためダイジェスト末尾 ({shortDigest}) を入力してください。";
        if (await DialogService.ShowOperationConfirm("イメージ削除", message, requireConfirmText: shortDigest) is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteImageAsync(image.Id, cancellationToken);
            Snackbar.AddSuccess($"{name} を削除しました。");
        }, LoadAsync);
    }
}
