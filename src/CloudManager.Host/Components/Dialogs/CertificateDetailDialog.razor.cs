namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class CertificateDetailDialog
{
    private CertificateDetail? detail;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string CertificateId { get; set; } = string.Empty;

    [Parameter]
    public string CertificateName { get; set; } = string.Empty;

    [Inject]
    public required CertificatesService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            detail = await Service.GetCertificateAsync(CertificateId, CancellationToken);
        });
}
