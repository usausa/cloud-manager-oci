namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class CertificatesPage
{
    private List<CertificateInfo> certificates = [];

    [Inject]
    public required CertificatesService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            certificates = await Service.ListCertificatesAsync(CancellationToken);
        });

    private async Task ShowDetailAsync(CertificateInfo certificate)
    {
        await DialogService.ShowAsync<CertificateDetailDialog>("証明書の詳細", new DialogParameters<CertificateDetailDialog>
        {
            { x => x.CertificateId, certificate.Id },
            { x => x.CertificateName, certificate.Name }
        },
        Styles.MediumDialog);
    }

    private static Color ExpiryColor(int days) => days switch
    {
        < 0 => Color.Error,
        < 30 => Color.Warning,
        _ => Color.Success
    };
}
