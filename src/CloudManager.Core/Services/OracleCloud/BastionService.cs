namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Bastion;

using Oci.BastionService;
using Oci.BastionService.Models;
using Oci.BastionService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class BastionService
{
    public const string SessionTypePortForwarding = "PORT_FORWARDING";
    public const string SessionTypeManagedSsh = "MANAGED_SSH";

    private readonly OciClientFactory factory;

    public BastionService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the bastions of the compartment
    public async ValueTask<List<BastionInfo>> ListBastionsAsync(CancellationToken cancellationToken = default)
    {
        using var bastion = factory.CreateBastionClient();
        var bastions = await OciPaging.ListAllAsync(
            page => bastion.ListBastions(new ListBastionsRequest { CompartmentId = factory.CompartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return bastions
            .Select(static x => new BastionInfo(
                x.Id,
                x.Name,
                x.BastionType,
                x.TargetVcnId,
                x.TargetSubnetId,
                OciValues.State(x.LifecycleState),
                x.TimeCreated))
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Sessions of a bastion, newest first
    public async ValueTask<List<BastionSessionInfo>> ListSessionsAsync(string bastionId, CancellationToken cancellationToken = default)
    {
        using var bastion = factory.CreateBastionClient();
        var sessions = await OciPaging.ListAllAsync(
            page => bastion.ListSessions(new ListSessionsRequest { BastionId = bastionId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return sessions
            .Select(static x => ToInfo(x.Id, x.DisplayName, x.TargetResourceDetails, x.LifecycleState, x.SessionTtlInSeconds, x.TimeCreated))
            .OrderByDescending(static x => x.TimeCreated)
            .ToList();
#pragma warning restore IDE0028
    }

    // Session with the SSH command provided by the service
    public async ValueTask<BastionSessionDetail> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var bastion = factory.CreateBastionClient();
        var response = await bastion.GetSession(new GetSessionRequest { SessionId = sessionId }, cancellationToken: cancellationToken);
        return ToDetail(response.Session);
    }

    // Creates a port forwarding or managed SSH session and waits until it is ACTIVE
    public async ValueTask<BastionSessionDetail> CreateSessionAsync(
        string bastionId,
        string displayName,
        string sessionType,
        string targetResourceId,
        string? targetPrivateIp,
        int targetPort,
        string? osUserName,
        string publicKey,
        int ttlSeconds,
        int timeoutSeconds,
        IProgress<ProgressUpdate> progress,
        CancellationToken cancellationToken = default)
    {
        using var bastion = factory.CreateBastionClient();

        CreateSessionTargetResourceDetails target = String.Equals(sessionType, SessionTypeManagedSsh, StringComparison.OrdinalIgnoreCase)
            ? new CreateManagedSshSessionTargetResourceDetails
            {
                TargetResourceId = targetResourceId,
                TargetResourceOperatingSystemUserName = osUserName,
                TargetResourcePrivateIpAddress = String.IsNullOrWhiteSpace(targetPrivateIp) ? null : targetPrivateIp,
                TargetResourcePort = targetPort
            }
            : new CreatePortForwardingSessionTargetResourceDetails
            {
                TargetResourceId = String.IsNullOrWhiteSpace(targetResourceId) ? null : targetResourceId,
                TargetResourcePrivateIpAddress = String.IsNullOrWhiteSpace(targetPrivateIp) ? null : targetPrivateIp,
                TargetResourcePort = targetPort
            };

        var response = await bastion.CreateSession(
            new CreateSessionRequest
            {
                CreateSessionDetails = new CreateSessionDetails
                {
                    BastionId = bastionId,
                    DisplayName = displayName,
                    KeyType = CreateSessionDetails.KeyTypeEnum.Pub,
                    KeyDetails = new PublicKeyDetails { PublicKeyContent = publicKey },
                    SessionTtlInSeconds = ttlSeconds,
                    TargetResourceDetails = target
                }
            },
            cancellationToken: cancellationToken);
        var sessionId = response.Session.Id;

        await OciPolling.WaitAsync(
            async ct =>
            {
                var session = await bastion.GetSession(new GetSessionRequest { SessionId = sessionId }, cancellationToken: ct);
                return OciValues.State(session.Session.LifecycleState);
            },
            static state => state is "ACTIVE" or "FAILED" or "DELETED",
            "ACTIVE",
            timeoutSeconds,
            progress,
            cancellationToken);

        var created = await bastion.GetSession(new GetSessionRequest { SessionId = sessionId }, cancellationToken: cancellationToken);
        return ToDetail(created.Session);
    }

    public async ValueTask DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var bastion = factory.CreateBastionClient();
        await bastion.DeleteSession(new DeleteSessionRequest { SessionId = sessionId }, cancellationToken: cancellationToken);
    }

    private static BastionSessionDetail ToDetail(Session session) =>
        new(
            ToInfo(session.Id, session.DisplayName, session.TargetResourceDetails, session.LifecycleState, session.SessionTtlInSeconds, session.TimeCreated),
            session.BastionUserName,
            session.SshMetadata?.GetValueOrDefault("command"));

    private static BastionSessionInfo ToInfo(string id, string displayName, TargetResourceDetails? target, SessionLifecycleState? state, int? ttl, DateTime? timeCreated)
    {
        var (type, resourceId, privateIp, port) = target switch
        {
            PortForwardingSessionTargetResourceDetails x => (SessionTypePortForwarding, x.TargetResourceId, x.TargetResourcePrivateIpAddress, x.TargetResourcePort),
            ManagedSshSessionTargetResourceDetails x => (SessionTypeManagedSsh, x.TargetResourceId, x.TargetResourcePrivateIpAddress, x.TargetResourcePort),
            DynamicPortForwardingSessionTargetResourceDetails => ("DYNAMIC_PORT_FORWARDING", null, null, null),
            _ => ("-", null, null, null)
        };
        return new BastionSessionInfo(id, displayName, type, resourceId, privateIp, port, OciValues.State(state), ttl, timeCreated);
    }
}
#pragma warning restore CA1724
