namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Certificates;

using Oci.CertificatesmanagementService.Requests;

public sealed class CertificatesService
{
    private readonly OciClientFactory factory;

    public CertificatesService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the certificates of the compartments in scope
    public async ValueTask<List<CertificateInfo>> ListCertificatesAsync(CancellationToken cancellationToken = default)
    {
        using var certificates = factory.CreateCertificatesManagementClient();
        var items = await factory.ListInScopeAsync(
            certificates,
            (client, compartmentId, page) => client.ListCertificates(new ListCertificatesRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.CertificateCollection.Items,
            static x => x.OpcNextPage,
            cancellationToken);

        var now = DateTime.UtcNow;
#pragma warning disable IDE0028
        return items
            .Select(x =>
            {
                var notAfter = x.CurrentVersionSummary?.Validity?.TimeOfValidityNotAfter;
                return new CertificateInfo(
                    x.Id,
                    x.CompartmentId,
                    x.Name,
                    OciValues.State(x.LifecycleState),
                    OciValues.State(x.ConfigType),
                    x.Subject?.CommonName,
                    notAfter,
                    notAfter.HasValue ? (int)(notAfter.Value - now).TotalDays : null,
                    x.TimeCreated);
            })
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Details of the current certificate version
    public async ValueTask<CertificateDetail> GetCertificateAsync(string certificateId, CancellationToken cancellationToken = default)
    {
        using var certificates = factory.CreateCertificatesManagementClient();
        var response = await certificates.GetCertificate(new GetCertificateRequest { CertificateId = certificateId }, cancellationToken: cancellationToken);
        var certificate = response.Certificate;
        var version = certificate.CurrentVersion;
#pragma warning disable IDE0028
        return new CertificateDetail(
            certificate.Id,
            certificate.Name,
            certificate.Description,
            certificate.Subject?.CommonName,
            (version?.SubjectAlternativeNames ?? []).Select(static x => $"{OciValues.State(x.Type)}:{x.Value}").ToList(),
            OciValues.State(certificate.ConfigType),
            certificate.IssuerCertificateAuthorityId,
            OciValues.State(certificate.KeyAlgorithm),
            OciValues.State(certificate.SignatureAlgorithm),
            version?.VersionNumber,
            version?.SerialNumber,
            version?.Validity?.TimeOfValidityNotBefore,
            version?.Validity?.TimeOfValidityNotAfter,
            (version?.Stages ?? []).Select(static x => OciValues.State(x)).ToList());
#pragma warning restore IDE0028
    }
}
