namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.ApiGateway;

using Oci.ApigatewayService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class ApiGatewayService
{
    private readonly OciClientFactory factory;

    public ApiGatewayService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the gateways of the compartments in scope
    public async ValueTask<List<ApiGatewayInfo>> ListGatewaysAsync(CancellationToken cancellationToken = default)
    {
        using var gateway = factory.CreateGatewayClient();
        var gateways = await factory.ListInScopeAsync(
            gateway,
            (client, compartmentId, page) => client.ListGateways(new ListGatewaysRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.GatewayCollection.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return gateways
            .Select(static x => new ApiGatewayInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                OciValues.State(x.EndpointType),
                x.Hostname,
                OciValues.State(x.LifecycleState),
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Deployments of a gateway; they may live in any compartment in scope
    public async ValueTask<List<ApiDeploymentInfo>> ListDeploymentsAsync(string gatewayId, CancellationToken cancellationToken = default)
    {
        using var deployment = factory.CreateDeploymentClient();
        var deployments = await factory.ListInScopeAsync(
            deployment,
            (client, compartmentId, page) => client.ListDeployments(new ListDeploymentsRequest { CompartmentId = compartmentId, GatewayId = gatewayId, Page = page }, cancellationToken: cancellationToken),
            static x => x.DeploymentCollection.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return deployments
            .Select(static x => new ApiDeploymentInfo(
                x.Id,
                x.DisplayName,
                x.PathPrefix,
                x.Endpoint,
                OciValues.State(x.LifecycleState),
                x.TimeUpdated))
            .OrderBy(static x => x.PathPrefix, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }
}
#pragma warning restore CA1724
