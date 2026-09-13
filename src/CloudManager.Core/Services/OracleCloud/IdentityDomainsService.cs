namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.IdentityDomains;

using Oci.IdentitydomainsService.Models;
using Oci.IdentitydomainsService.Requests;

using ListDomainsRequest = Oci.IdentityService.Requests.ListDomainsRequest;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class IdentityDomainsService
{
    private const int PageSize = 100;

    private const string UserAttributes = "userName,displayName,active,emails,meta";

    private readonly OciClientFactory factory;

    public IdentityDomainsService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the identity domains of the compartment, falling back to the tenancy root
    public async ValueTask<List<IdentityDomainInfo>> ListDomainsAsync(CancellationToken cancellationToken = default)
    {
        using var identity = factory.CreateIdentityClient();
        var domains = await ListDomainsAsync(identity, factory.CompartmentId, cancellationToken);
        if ((domains.Count == 0) && !String.Equals(factory.CompartmentId, factory.TenancyId, StringComparison.Ordinal))
        {
            domains = await ListDomainsAsync(identity, factory.TenancyId, cancellationToken);
        }

        return domains;
    }

    // Users of a domain through its SCIM endpoint
    public async ValueTask<List<DomainUserInfo>> ListUsersAsync(string domainEndpoint, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateIdentityDomainsClient(domainEndpoint);
        var result = new List<DomainUserInfo>();
        var startIndex = 1;
        while (true)
        {
            var response = await client.ListUsers(
                new ListUsersRequest { Attributes = UserAttributes, Count = PageSize, StartIndex = startIndex },
                cancellationToken: cancellationToken);
            var users = response.Users;
            result.AddRange((users.Resources ?? []).Select(ToInfo));

            var total = users.TotalResults ?? 0;
            var received = users.Resources?.Count ?? 0;
            if ((received == 0) || (startIndex + received > total))
            {
                break;
            }

            startIndex += received;
        }

        result.Sort(static (x, y) => String.Compare(x.UserName, y.UserName, StringComparison.Ordinal));
        return result;
    }

    // Sends the password reset notification to the user
    public async ValueTask ResetPasswordAsync(string domainEndpoint, string userId, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateIdentityDomainsClient(domainEndpoint);
        await client.PutUserPasswordResetter(
            new PutUserPasswordResetterRequest
            {
                UserPasswordResetterId = userId,
                UserPasswordResetter = new UserPasswordResetter { Schemas = ["urn:ietf:params:scim:schemas:oracle:idcs:UserPasswordResetter"] }
            },
            cancellationToken: cancellationToken);
    }

    // Sets a password directly without notifying the user
    public async ValueTask SetPasswordAsync(string domainEndpoint, string userId, string password, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateIdentityDomainsClient(domainEndpoint);
        await client.PutUserPasswordChanger(
            new PutUserPasswordChangerRequest
            {
                UserPasswordChangerId = userId,
                UserPasswordChanger = new UserPasswordChanger
                {
                    Schemas = ["urn:ietf:params:scim:schemas:oracle:idcs:UserPasswordChanger"],
                    Password = password,
                    BypassNotification = true
                }
            },
            cancellationToken: cancellationToken);
    }

    private static async ValueTask<List<IdentityDomainInfo>> ListDomainsAsync(Oci.IdentityService.IdentityClient identity, string compartmentId, CancellationToken cancellationToken)
    {
        var domains = await OciPaging.ListAllAsync(
            page => identity.ListDomains(new ListDomainsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return domains
            .Select(static x => new IdentityDomainInfo(
                x.Id,
                x.DisplayName,
                x.Url,
                OciValues.State(x.Type),
                x.LicenseType,
                OciValues.State(x.LifecycleState),
                x.HomeRegion,
                x.TimeCreated))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    private static DomainUserInfo ToInfo(User user)
    {
        var email = user.Emails?.FirstOrDefault(static x => x.Primary ?? false)?.Value ?? user.Emails?.FirstOrDefault()?.Value;
        DateTime? created = DateTime.TryParse(user.Meta?.Created, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var value) ? value : null;
        return new DomainUserInfo(user.Id, user.UserName, user.DisplayName, user.Active ?? false, email, created);
    }
}
#pragma warning restore CA1724
