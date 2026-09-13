namespace CloudManager.Infrastructure.OracleCloud;

using Oci.Common;
using Oci.ComputeinstanceagentService;
using Oci.ContainerinstancesService;
using Oci.CoreService;
using Oci.DatabaseService;
using Oci.FunctionsService;
using Oci.IdentityService;
using Oci.MonitoringService;
using Oci.ResourcesearchService;
using Oci.WorkrequestsService;

// Creates OCI clients, resolving the profile, region and compartment on every call
public sealed class OciClientFactory
{
    private readonly Func<OciContext> resolver;

    public OciClientFactory(Func<OciContext> resolver)
    {
        this.resolver = resolver;
    }

    // Creates a factory bound to a fixed profile, region and compartment (for job execution and API endpoints)
    public static OciClientFactory Create(string profileName, string? regionId, string? compartmentId) =>
        new(() => ProfileResolver.Resolve(profileName, regionId, compartmentId));

    public string TenancyId => Resolve().TenancyId;

    public string CompartmentId => Resolve().CompartmentId;

    public Region Region => Resolve().Region;

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

    public IdentityClient CreateIdentityClient() => Configure(new IdentityClient(Resolve().Provider));

    public ResourceSearchClient CreateResourceSearchClient() => Configure(new ResourceSearchClient(Resolve().Provider));

    //--------------------------------------------------------------------------------
    // Compute
    //--------------------------------------------------------------------------------

    public ComputeClient CreateComputeClient() => Configure(new ComputeClient(Resolve().Provider));

    public VirtualNetworkClient CreateVirtualNetworkClient() => Configure(new VirtualNetworkClient(Resolve().Provider));

    public ComputeInstanceAgentClient CreateComputeInstanceAgentClient() => Configure(new ComputeInstanceAgentClient(Resolve().Provider));

    public ContainerInstanceClient CreateContainerInstanceClient() => Configure(new ContainerInstanceClient(Resolve().Provider));

    public FunctionsManagementClient CreateFunctionsManagementClient() => Configure(new FunctionsManagementClient(Resolve().Provider));

    // The invoke endpoint differs per function
    public FunctionsInvokeClient CreateFunctionsInvokeClient(string invokeEndpoint)
    {
        var client = new FunctionsInvokeClient(Resolve().Provider);
        client.SetEndpoint(invokeEndpoint);
        return client;
    }

    //--------------------------------------------------------------------------------
    // Database
    //--------------------------------------------------------------------------------

    public DatabaseClient CreateDatabaseClient() => Configure(new DatabaseClient(Resolve().Provider));

    public WorkRequestClient CreateWorkRequestClient() => Configure(new WorkRequestClient(Resolve().Provider));

    //--------------------------------------------------------------------------------
    // Monitoring
    //--------------------------------------------------------------------------------

    public MonitoringClient CreateMonitoringClient() => Configure(new MonitoringClient(Resolve().Provider));
}
