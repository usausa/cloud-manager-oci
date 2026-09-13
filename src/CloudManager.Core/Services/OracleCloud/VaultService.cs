namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Vault;

using Oci.SecretsService.Models;
using Oci.SecretsService.Requests;
using Oci.VaultService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class VaultService
{
    private readonly OciClientFactory factory;

    public VaultService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the secrets of the compartments in scope
    public async ValueTask<List<SecretInfo>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        using var vaults = factory.CreateVaultsClient();
        var secrets = await factory.ListInScopeAsync(
            vaults,
            (client, compartmentId, page) => client.ListSecrets(new ListSecretsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return secrets
            .Select(static x => new SecretInfo(
                x.Id,
                x.CompartmentId,
                x.SecretName,
                x.VaultId,
                OciValues.State(x.LifecycleState),
                x.Description,
                OciValues.State(x.RotationStatus),
                x.TimeCreated,
                x.TimeOfCurrentVersionExpiry,
                x.LastRotationTime))
            .OrderBy(static x => x.SecretName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Current value of a secret (base64 content decoded)
    public async ValueTask<SecretValueInfo> GetSecretValueAsync(string secretId, string secretName, CancellationToken cancellationToken = default)
    {
        using var secrets = factory.CreateSecretsClient();
        var response = await secrets.GetSecretBundle(
            new GetSecretBundleRequest { SecretId = secretId, Stage = GetSecretBundleRequest.StageEnum.Current },
            cancellationToken: cancellationToken);
        var bundle = response.SecretBundle;

        var content = bundle.SecretBundleContent is Base64SecretBundleContentDetails base64
            ? Encoding.UTF8.GetString(Convert.FromBase64String(base64.Content))
            : string.Empty;
        return new SecretValueInfo(secretName, content, bundle.VersionNumber, bundle.VersionName, bundle.TimeCreated);
    }

    // Triggers a rotation through the configured target system
    public async ValueTask RotateSecretAsync(string secretId, CancellationToken cancellationToken = default)
    {
        using var vaults = factory.CreateVaultsClient();
        await vaults.RotateSecret(new RotateSecretRequest { SecretId = secretId }, cancellationToken: cancellationToken);
    }
}
#pragma warning restore CA1724
