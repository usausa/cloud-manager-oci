namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.LoadBalancer;

using Oci.LoadbalancerService.Requests;

using Nlb = Oci.NetworkloadbalancerService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class LoadBalancerService
{
    public const string TypeLoadBalancer = "LB";
    public const string TypeNetworkLoadBalancer = "NLB";

    private readonly OciClientFactory factory;

    public LoadBalancerService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists load balancers and network load balancers of the compartments in scope
    public async ValueTask<List<LoadBalancerInfo>> ListLoadBalancersAsync(CancellationToken cancellationToken = default)
    {
        using var lb = factory.CreateLoadBalancerClient();
        using var nlb = factory.CreateNetworkLoadBalancerClient();

        var loadBalancers = await factory.ListInScopeAsync(
            lb,
            (client, compartmentId, page) => client.ListLoadBalancers(new ListLoadBalancersRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage,
            cancellationToken);
        var networkLoadBalancers = await factory.ListInScopeAsync(
            nlb,
            (client, compartmentId, page) => client.ListNetworkLoadBalancers(new Nlb.ListNetworkLoadBalancersRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.NetworkLoadBalancerCollection.Items,
            static x => x.OpcNextPage,
            cancellationToken);

        var result = loadBalancers
            .Select(static x => new LoadBalancerInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                TypeLoadBalancer,
                OciValues.State(x.LifecycleState),
                x.ShapeName,
                x.IsPrivate ?? false,
                String.Join(", ", (x.IpAddresses ?? []).Select(static ip => ip.IpAddressProp)),
                x.BackendSets?.Count ?? 0,
                x.Listeners?.Count ?? 0,
                x.TimeCreated.GetValueOrDefault()))
            .Concat(networkLoadBalancers.Select(static x => new LoadBalancerInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                TypeNetworkLoadBalancer,
                OciValues.State(x.LifecycleState),
                null,
                x.IsPrivate ?? false,
                String.Join(", ", (x.IpAddresses ?? []).Select(static ip => ip.IpAddressProp)),
                x.BackendSets?.Count ?? 0,
                x.Listeners?.Count ?? 0,
                x.TimeCreated.GetValueOrDefault())))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
        return result;
    }

    // Backend sets with their overall health
    public async ValueTask<List<BackendSetInfo>> ListBackendSetsAsync(string loadBalancerId, string type, CancellationToken cancellationToken = default)
    {
        var result = new List<BackendSetInfo>();
        if (IsNetworkLoadBalancer(type))
        {
            using var nlb = factory.CreateNetworkLoadBalancerClient();
            var sets = await OciPaging.ListAllAsync(
                page => nlb.ListBackendSets(new Nlb.ListBackendSetsRequest { NetworkLoadBalancerId = loadBalancerId, Page = page }, cancellationToken: cancellationToken),
                static x => x.BackendSetCollection.Items,
                static x => x.OpcNextPage);
            foreach (var set in sets)
            {
                var health = await nlb.GetBackendSetHealth(new Nlb.GetBackendSetHealthRequest { NetworkLoadBalancerId = loadBalancerId, BackendSetName = set.Name }, cancellationToken: cancellationToken);
                result.Add(new BackendSetInfo(
                    set.Name,
                    OciValues.State(set.Policy),
                    set.Backends?.Count ?? 0,
                    OciValues.State(health.BackendSetHealth.Status),
                    OciValues.State(set.HealthChecker?.Protocol),
                    set.HealthChecker?.Port,
                    set.HealthChecker?.UrlPath));
            }
        }
        else
        {
            using var lb = factory.CreateLoadBalancerClient();
            var sets = await lb.ListBackendSets(new ListBackendSetsRequest { LoadBalancerId = loadBalancerId }, cancellationToken: cancellationToken);
            foreach (var set in sets.Items)
            {
                var health = await lb.GetBackendSetHealth(new GetBackendSetHealthRequest { LoadBalancerId = loadBalancerId, BackendSetName = set.Name }, cancellationToken: cancellationToken);
                result.Add(new BackendSetInfo(
                    set.Name,
                    set.Policy,
                    set.Backends?.Count ?? 0,
                    OciValues.State(health.BackendSetHealth.Status),
                    set.HealthChecker?.Protocol,
                    set.HealthChecker?.Port,
                    set.HealthChecker?.UrlPath));
            }
        }

        result.Sort(static (x, y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));
        return result;
    }

    // Backends of a backend set with their health
    public async ValueTask<List<BackendHealthInfo>> ListBackendHealthAsync(string loadBalancerId, string type, string backendSetName, CancellationToken cancellationToken = default)
    {
        var result = new List<BackendHealthInfo>();
        if (IsNetworkLoadBalancer(type))
        {
            using var nlb = factory.CreateNetworkLoadBalancerClient();
            var backends = await OciPaging.ListAllAsync(
                page => nlb.ListBackends(new Nlb.ListBackendsRequest { NetworkLoadBalancerId = loadBalancerId, BackendSetName = backendSetName, Page = page }, cancellationToken: cancellationToken),
                static x => x.BackendCollection.Items,
                static x => x.OpcNextPage);
            foreach (var backend in backends)
            {
                var health = await nlb.GetBackendHealth(new Nlb.GetBackendHealthRequest { NetworkLoadBalancerId = loadBalancerId, BackendSetName = backendSetName, BackendName = backend.Name }, cancellationToken: cancellationToken);
                result.Add(new BackendHealthInfo(
                    backend.Name,
                    backend.IpAddress,
                    backend.Port ?? 0,
                    OciValues.State(health.BackendHealth.Status),
                    backend.Weight ?? 0,
                    backend.IsOffline ?? false,
                    backend.IsDrain ?? false,
                    backend.IsBackup ?? false));
            }
        }
        else
        {
            using var lb = factory.CreateLoadBalancerClient();
            var backends = await lb.ListBackends(new ListBackendsRequest { LoadBalancerId = loadBalancerId, BackendSetName = backendSetName }, cancellationToken: cancellationToken);
            foreach (var backend in backends.Items)
            {
                var health = await lb.GetBackendHealth(new GetBackendHealthRequest { LoadBalancerId = loadBalancerId, BackendSetName = backendSetName, BackendName = backend.Name }, cancellationToken: cancellationToken);
                result.Add(new BackendHealthInfo(
                    backend.Name,
                    backend.IpAddress,
                    backend.Port ?? 0,
                    OciValues.State(health.BackendHealth.Status),
                    backend.Weight ?? 0,
                    backend.Offline ?? false,
                    backend.Drain ?? false,
                    backend.Backup ?? false));
            }
        }

        return result;
    }

    private static bool IsNetworkLoadBalancer(string type) =>
        String.Equals(type, TypeNetworkLoadBalancer, StringComparison.OrdinalIgnoreCase);
}
#pragma warning restore CA1724
