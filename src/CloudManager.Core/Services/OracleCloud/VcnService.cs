namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Vcn;

using Oci.CoreService.Requests;

public sealed class VcnService
{
    private readonly OciClientFactory factory;

    public VcnService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the VCNs of the compartments in scope
    public async ValueTask<List<VcnInfo>> ListVcnsAsync(CancellationToken cancellationToken = default)
    {
        using var network = factory.CreateVirtualNetworkClient();
        var vcns = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => network.ListVcns(new ListVcnsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);

#pragma warning disable IDE0028
        return vcns
            .Select(static x => new VcnInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                String.Join(", ", x.CidrBlocks ?? [x.CidrBlock]),
                OciValues.State(x.LifecycleState),
                x.DnsLabel,
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Loads the components of a VCN in parallel; they are listed in the compartment of the VCN
    public async ValueTask<VcnDetail> GetVcnDetailAsync(string vcnId, CancellationToken cancellationToken = default)
    {
        using var network = factory.CreateVirtualNetworkClient();

        var vcnResponse = await network.GetVcn(new GetVcnRequest { VcnId = vcnId }, cancellationToken: cancellationToken);
        var vcn = vcnResponse.Vcn;
        var compartmentId = vcn.CompartmentId;

        var subnetsTask = OciPaging.ListAllAsync(
            page => network.ListSubnets(new ListSubnetsRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        var routeTablesTask = OciPaging.ListAllAsync(
            page => network.ListRouteTables(new ListRouteTablesRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        var securityListsTask = OciPaging.ListAllAsync(
            page => network.ListSecurityLists(new ListSecurityListsRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        var nsgsTask = OciPaging.ListAllAsync(
            page => network.ListNetworkSecurityGroups(new ListNetworkSecurityGroupsRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        var igwsTask = OciPaging.ListAllAsync(
            page => network.ListInternetGateways(new ListInternetGatewaysRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        var natsTask = OciPaging.ListAllAsync(
            page => network.ListNatGateways(new ListNatGatewaysRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        var sgwsTask = OciPaging.ListAllAsync(
            page => network.ListServiceGateways(new ListServiceGatewaysRequest { CompartmentId = compartmentId, VcnId = vcnId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage).AsTask();
        await Task.WhenAll(subnetsTask, routeTablesTask, securityListsTask, nsgsTask, igwsTask, natsTask, sgwsTask);

#pragma warning disable IDE0028
        return new VcnDetail(
            new VcnInfo(vcn.Id, vcn.CompartmentId, vcn.DisplayName, String.Join(", ", vcn.CidrBlocks ?? [vcn.CidrBlock]), OciValues.State(vcn.LifecycleState), vcn.DnsLabel, vcn.TimeCreated.GetValueOrDefault()),
            (await subnetsTask)
                .Select(static x => new SubnetInfo(x.Id, x.DisplayName, x.CidrBlock, x.AvailabilityDomain, !(x.ProhibitPublicIpOnVnic ?? false), OciValues.State(x.LifecycleState)))
                .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
                .ToList(),
            (await routeTablesTask)
                .Select(x => new RouteTableInfo(x.Id, x.DisplayName, x.Id == vcn.DefaultRouteTableId, x.RouteRules?.Count ?? 0, OciValues.State(x.LifecycleState)))
                .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
                .ToList(),
            (await securityListsTask)
                .Select(x => new SecurityListInfo(x.Id, x.DisplayName, x.Id == vcn.DefaultSecurityListId, x.IngressSecurityRules?.Count ?? 0, x.EgressSecurityRules?.Count ?? 0, OciValues.State(x.LifecycleState)))
                .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
                .ToList(),
            (await nsgsTask)
                .Select(static x => new NetworkSecurityGroupInfo(x.Id, x.DisplayName, OciValues.State(x.LifecycleState)))
                .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
                .ToList(),
            (await igwsTask)
                .Select(static x => new InternetGatewayInfo(x.Id, x.DisplayName, x.IsEnabled ?? false, OciValues.State(x.LifecycleState)))
                .ToList(),
            (await natsTask)
                .Select(static x => new NatGatewayInfo(x.Id, x.DisplayName, x.NatIp, x.BlockTraffic ?? false, OciValues.State(x.LifecycleState)))
                .ToList(),
            (await sgwsTask)
                .Select(static x => new ServiceGatewayInfo(x.Id, x.DisplayName, String.Join(", ", (x.Services ?? []).Select(static s => s.ServiceName)), OciValues.State(x.LifecycleState)))
                .ToList());
#pragma warning restore IDE0028
    }
}
