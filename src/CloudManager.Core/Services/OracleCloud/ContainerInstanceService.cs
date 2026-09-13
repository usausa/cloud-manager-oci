namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.ContainerInstance;

using Oci.ContainerinstancesService;
using Oci.ContainerinstancesService.Requests;

public sealed class ContainerInstanceService
{
    private readonly OciClientFactory factory;

    public ContainerInstanceService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the container instances of the compartments in scope
    public async ValueTask<List<ContainerInstanceInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var containers = factory.CreateContainerInstanceClient();
        var instances = await factory.ListInScopeAsync(
            containers,
            (client, compartmentId, page) => client.ListContainerInstances(new ListContainerInstancesRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.ContainerInstanceCollection.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return instances
            .Select(static x => new ContainerInstanceInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                OciValues.State(x.LifecycleState),
                x.Shape,
                x.ShapeConfig?.Ocpus,
                x.ShapeConfig?.MemoryInGBs,
                x.ContainerCount ?? 0,
                x.AvailabilityDomain,
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Starts a container instance, polling until ACTIVE when wait is set
    public async ValueTask StartAsync(string containerInstanceId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateContainerInstanceClient();
        await client.StartContainerInstance(new StartContainerInstanceRequest { ContainerInstanceId = containerInstanceId }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(client, containerInstanceId, "ACTIVE", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Stops a container instance, polling until INACTIVE when wait is set
    public async ValueTask StopAsync(string containerInstanceId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateContainerInstanceClient();
        await client.StopContainerInstance(new StopContainerInstanceRequest { ContainerInstanceId = containerInstanceId }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(client, containerInstanceId, "INACTIVE", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Restarts a container instance, polling until ACTIVE when wait is set
    public async ValueTask RestartAsync(string containerInstanceId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateContainerInstanceClient();
        await client.RestartContainerInstance(new RestartContainerInstanceRequest { ContainerInstanceId = containerInstanceId }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(client, containerInstanceId, "ACTIVE", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Containers of an instance, listed in the compartment of the instance
    public async ValueTask<List<ContainerInfo>> ListContainersAsync(string containerInstanceId, CancellationToken cancellationToken = default)
    {
        using var client = factory.CreateContainerInstanceClient();
        var instance = await client.GetContainerInstance(new GetContainerInstanceRequest { ContainerInstanceId = containerInstanceId }, cancellationToken: cancellationToken);
        var containers = await OciPaging.ListAllAsync(
            page => client.ListContainers(new ListContainersRequest { CompartmentId = instance.ContainerInstance.CompartmentId, ContainerInstanceId = containerInstanceId, Page = page }, cancellationToken: cancellationToken),
            static x => x.ContainerCollection.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return containers
            .Select(static x => new ContainerInfo(
                x.Id,
                x.DisplayName,
                OciValues.State(x.LifecycleState),
                x.ImageUrl,
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    private static ValueTask WaitForStateAsync(ContainerInstanceClient client, string containerInstanceId, string targetState, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken) =>
        OciPolling.WaitForStateAsync(
            async ct =>
            {
                var response = await client.GetContainerInstance(new GetContainerInstanceRequest { ContainerInstanceId = containerInstanceId }, cancellationToken: ct);
                return OciValues.State(response.ContainerInstance.LifecycleState);
            },
            targetState,
            timeoutSeconds,
            progress,
            cancellationToken);
}
