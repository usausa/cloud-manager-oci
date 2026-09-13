namespace CloudManager.Infrastructure.OracleCloud;

using Oci.ApigatewayService;
using Oci.ArtifactsService;
using Oci.BastionService;
using Oci.CertificatesmanagementService;
using Oci.Common;
using Oci.Common.Retry;
using Oci.ComputeinstanceagentService;
using Oci.ContainerinstancesService;
using Oci.CoreService;
using Oci.DatabaseService;
using Oci.DnsService;
using Oci.EventsService;
using Oci.FunctionsService;
using Oci.IdentitydomainsService;
using Oci.IdentityService;
using Oci.LoadbalancerService;
using Oci.LoggingsearchService;
using Oci.LoggingService;
using Oci.MonitoringService;
using Oci.NetworkloadbalancerService;
using Oci.NosqlService;
using Oci.ObjectstorageService;
using Oci.ObjectstorageService.Requests;
using Oci.OnsService;
using Oci.QueueService;
using Oci.ResourcesearchService;
using Oci.SecretsService;
using Oci.UsageapiService;
using Oci.VaultService;
using Oci.WorkrequestsService;

// Creates OCI clients, resolving the profile, region and compartment on every call
public sealed class OciClientFactory
{
    private const int MaxParallelCompartments = 4;

    // Throttled (429) and transient server errors are retried with a short backoff instead of failing the page;
    // listings fan out over compartments, so rate limits are hit more easily than with single calls
    private static readonly ClientConfiguration ClientConfiguration = new()
    {
        RetryConfiguration = new RetryConfiguration
        {
            MaxAttempts = 5,
            TotalElapsedTimeInSecs = 60,
            GetNextDelayInSeconds = static attempt => Math.Min(Math.Pow(2, attempt), 8)
        }
    };

    private readonly Func<OciContext> resolver;

    private string? namespaceTenancyId;

    private string? namespaceName;

    public OciClientFactory(Func<OciContext> resolver)
    {
        this.resolver = resolver;
    }

    // Creates a factory bound to a fixed profile, region and compartment (for job execution and API endpoints)
    public static OciClientFactory Create(string profileName, string? regionId, string? compartmentId) =>
        new(() => ProfileResolver.Resolve(profileName, regionId, compartmentId));

    public string TenancyId => Resolve().TenancyId;

    public string CompartmentId => Resolve().CompartmentId;

    // The compartments a listing covers: the selected one and its descendants
    public IReadOnlyList<string> CompartmentIds => Resolve().CompartmentIds;

    public Region Region => Resolve().Region;

    // Runs a per-compartment listing over every compartment in scope and flattens the results
    public async ValueTask<List<T>> ListInScopeAsync<T>(Func<string, ValueTask<List<T>>> list, CancellationToken cancellationToken)
    {
        var compartmentIds = CompartmentIds;
        if (compartmentIds.Count == 1)
        {
            return await list(compartmentIds[0]);
        }

        var results = await OciParallel.MapAsync(compartmentIds, MaxParallelCompartments, list, cancellationToken);
        return [.. results.SelectMany(static x => x)];
    }

    // Lists every page of a compartment-scoped operation in every compartment in scope; the client is passed
    // through so that the deferred fetch does not have to capture a disposable local
    public ValueTask<List<TItem>> ListInScopeAsync<TClient, TResponse, TItem>(
        TClient client,
        Func<TClient, string, string?, Task<TResponse>> fetch,
        Func<TResponse, IEnumerable<TItem>?> items,
        Func<TResponse, string?> nextPage,
        CancellationToken cancellationToken) =>
        ListInScopeAsync(compartmentId => OciPaging.ListAllAsync(page => fetch(client, compartmentId, page), items, nextPage), cancellationToken);

    private OciContext Resolve() => resolver();

    // The client region follows the resolved context, not the profile default
    private T Configure<T>(T client)
        where T : RegionalClientBase
    {
        client.SetRegion(Resolve().Region);
        return client;
    }

    //--------------------------------------------------------------------------------
    // Identity / Search
    //--------------------------------------------------------------------------------

    public IdentityClient CreateIdentityClient() => Configure(new IdentityClient(Resolve().Provider, ClientConfiguration));

    public ResourceSearchClient CreateResourceSearchClient() => Configure(new ResourceSearchClient(Resolve().Provider, ClientConfiguration));

    //--------------------------------------------------------------------------------
    // Compute
    //--------------------------------------------------------------------------------

    public ComputeClient CreateComputeClient() => Configure(new ComputeClient(Resolve().Provider, ClientConfiguration));

    public VirtualNetworkClient CreateVirtualNetworkClient() => Configure(new VirtualNetworkClient(Resolve().Provider, ClientConfiguration));

    public BlockstorageClient CreateBlockstorageClient() => Configure(new BlockstorageClient(Resolve().Provider, ClientConfiguration));

    public ComputeInstanceAgentClient CreateComputeInstanceAgentClient() => Configure(new ComputeInstanceAgentClient(Resolve().Provider, ClientConfiguration));

    public ContainerInstanceClient CreateContainerInstanceClient() => Configure(new ContainerInstanceClient(Resolve().Provider, ClientConfiguration));

    public FunctionsManagementClient CreateFunctionsManagementClient() => Configure(new FunctionsManagementClient(Resolve().Provider, ClientConfiguration));

    // The invoke endpoint differs per function
    public FunctionsInvokeClient CreateFunctionsInvokeClient(string invokeEndpoint)
    {
        var client = new FunctionsInvokeClient(Resolve().Provider, ClientConfiguration);
        client.SetEndpoint(invokeEndpoint);
        return client;
    }

    //--------------------------------------------------------------------------------
    // Container / Storage
    //--------------------------------------------------------------------------------

    public ArtifactsClient CreateArtifactsClient() => Configure(new ArtifactsClient(Resolve().Provider, ClientConfiguration));

    public ObjectStorageClient CreateObjectStorageClient() => Configure(new ObjectStorageClient(Resolve().Provider, ClientConfiguration));

    // The Object Storage namespace is fixed per tenancy, so it is resolved once
    public async ValueTask<string> GetNamespaceAsync(CancellationToken cancellationToken = default)
    {
        var tenancyId = TenancyId;
        if ((namespaceName is not null) && String.Equals(namespaceTenancyId, tenancyId, StringComparison.Ordinal))
        {
            return namespaceName;
        }

        using var client = CreateObjectStorageClient();
        var response = await client.GetNamespace(new GetNamespaceRequest(), cancellationToken: cancellationToken);
        namespaceName = response.Value;
        namespaceTenancyId = tenancyId;
        return namespaceName;
    }

    //--------------------------------------------------------------------------------
    // Database
    //--------------------------------------------------------------------------------

    public DatabaseClient CreateDatabaseClient() => Configure(new DatabaseClient(Resolve().Provider, ClientConfiguration));

    public WorkRequestClient CreateWorkRequestClient() => Configure(new WorkRequestClient(Resolve().Provider, ClientConfiguration));

    public NosqlClient CreateNosqlClient() => Configure(new NosqlClient(Resolve().Provider, ClientConfiguration));

    //--------------------------------------------------------------------------------
    // Network
    //--------------------------------------------------------------------------------

    public LoadBalancerClient CreateLoadBalancerClient() => Configure(new LoadBalancerClient(Resolve().Provider, ClientConfiguration));

    public NetworkLoadBalancerClient CreateNetworkLoadBalancerClient() => Configure(new NetworkLoadBalancerClient(Resolve().Provider, ClientConfiguration));

    public DnsClient CreateDnsClient() => Configure(new DnsClient(Resolve().Provider, ClientConfiguration));

    public CertificatesManagementClient CreateCertificatesManagementClient() => Configure(new CertificatesManagementClient(Resolve().Provider, ClientConfiguration));

    //--------------------------------------------------------------------------------
    // API / Messaging
    //--------------------------------------------------------------------------------

    public GatewayClient CreateGatewayClient() => Configure(new GatewayClient(Resolve().Provider, ClientConfiguration));

    public DeploymentClient CreateDeploymentClient() => Configure(new DeploymentClient(Resolve().Provider, ClientConfiguration));

    public EventsClient CreateEventsClient() => Configure(new EventsClient(Resolve().Provider, ClientConfiguration));

    public QueueAdminClient CreateQueueAdminClient() => Configure(new QueueAdminClient(Resolve().Provider, ClientConfiguration));

    // The messages endpoint differs per queue
    public QueueClient CreateQueueClient(string messagesEndpoint)
    {
        var client = new QueueClient(Resolve().Provider, ClientConfiguration);
        client.SetEndpoint(messagesEndpoint);
        return client;
    }

    public NotificationControlPlaneClient CreateNotificationControlPlaneClient() => Configure(new NotificationControlPlaneClient(Resolve().Provider, ClientConfiguration));

    public NotificationDataPlaneClient CreateNotificationDataPlaneClient() => Configure(new NotificationDataPlaneClient(Resolve().Provider, ClientConfiguration));

    //--------------------------------------------------------------------------------
    // Monitoring / Logging
    //--------------------------------------------------------------------------------

    public MonitoringClient CreateMonitoringClient() => Configure(new MonitoringClient(Resolve().Provider, ClientConfiguration));

    public LoggingManagementClient CreateLoggingManagementClient() => Configure(new LoggingManagementClient(Resolve().Provider, ClientConfiguration));

    public LogSearchClient CreateLogSearchClient() => Configure(new LogSearchClient(Resolve().Provider, ClientConfiguration));

    //--------------------------------------------------------------------------------
    // Security / Identity
    //--------------------------------------------------------------------------------

    public VaultsClient CreateVaultsClient() => Configure(new VaultsClient(Resolve().Provider, ClientConfiguration));

    public SecretsClient CreateSecretsClient() => Configure(new SecretsClient(Resolve().Provider, ClientConfiguration));

    // Identity domains are addressed by their own endpoint
    public IdentityDomainsClient CreateIdentityDomainsClient(string domainEndpoint)
    {
        var client = new IdentityDomainsClient(Resolve().Provider, ClientConfiguration);
        client.SetEndpoint(domainEndpoint);
        return client;
    }

    public BastionClient CreateBastionClient() => Configure(new BastionClient(Resolve().Provider, ClientConfiguration));

    //--------------------------------------------------------------------------------
    // Cost
    //--------------------------------------------------------------------------------

    public UsageapiClient CreateUsageapiClient() => Configure(new UsageapiClient(Resolve().Provider, ClientConfiguration));
}
