namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Identity;

using Oci.IdentityService.Models;
using Oci.IdentityService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class IdentityService
{
    private readonly OciClientFactory factory;

    public IdentityService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the tenancy root and every accessible active compartment below it, sorted by path
    public async ValueTask<List<CompartmentInfo>> ListCompartmentsAsync(CancellationToken cancellationToken = default)
    {
        using var identity = factory.CreateIdentityClient();
        var tenancyId = factory.TenancyId;

        var tenancy = await identity.GetTenancy(new GetTenancyRequest { TenancyId = tenancyId }, cancellationToken: cancellationToken);
        var compartments = await OciPaging.ListAllAsync(
            page => identity.ListCompartments(
                new ListCompartmentsRequest
                {
                    CompartmentId = tenancyId,
                    CompartmentIdInSubtree = true,
                    AccessLevel = ListCompartmentsRequest.AccessLevelEnum.Accessible,
                    LifecycleState = Compartment.LifecycleStateEnum.Active,
                    Page = page
                },
                cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

        var byId = compartments.ToDictionary(static x => x.Id, static x => x);
        var root = new CompartmentInfo(tenancyId, tenancy.Tenancy.Name, tenancy.Tenancy.Name, null, 0);
        var result = new List<CompartmentInfo> { root };
        result.AddRange(compartments.Select(x => ToInfo(x, root.Name, byId)));
        result.Sort(static (x, y) => String.Compare(x.Path, y.Path, StringComparison.Ordinal));
        return result;
    }

    // Regions the tenancy is subscribed to
    public async ValueTask<List<RegionSubscriptionInfo>> ListRegionSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        using var identity = factory.CreateIdentityClient();
        var response = await identity.ListRegionSubscriptions(new ListRegionSubscriptionsRequest { TenancyId = factory.TenancyId }, cancellationToken: cancellationToken);
#pragma warning disable IDE0028
        return response.Items
            .Select(static x => new RegionSubscriptionInfo(x.RegionName, x.RegionKey, x.IsHomeRegion ?? false, OciValues.State(x.Status)))
            .OrderByDescending(static x => x.IsHomeRegion)
            .ThenBy(static x => x.RegionName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    private static CompartmentInfo ToInfo(Compartment compartment, string rootName, Dictionary<string, Compartment> byId)
    {
        var names = new List<string> { compartment.Name };
        var depth = 1;
        var parentId = compartment.CompartmentId;
        while (!String.IsNullOrEmpty(parentId) && byId.TryGetValue(parentId, out var parent))
        {
            names.Insert(0, parent.Name);
            depth++;
            parentId = parent.CompartmentId;
        }

        names.Insert(0, rootName);
        return new CompartmentInfo(compartment.Id, compartment.Name, String.Join(" / ", names), compartment.CompartmentId, depth);
    }
}
#pragma warning restore CA1724
