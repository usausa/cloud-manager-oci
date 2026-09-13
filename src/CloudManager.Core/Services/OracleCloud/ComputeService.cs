namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Compute;

using Oci.Common.Model;
using Oci.ComputeinstanceagentService.Models;
using Oci.ComputeinstanceagentService.Requests;
using Oci.CoreService;
using Oci.CoreService.Models;
using Oci.CoreService.Requests;

public sealed class ComputeService
{
    private const int MaxParallelLookups = 8;

    private readonly OciClientFactory factory;

    public ComputeService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the instances of the compartments in scope with their primary VNIC addresses
    public async ValueTask<List<ComputeInstanceInfo>> ListInstancesAsync(string? state, string? tag, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        using var network = factory.CreateVirtualNetworkClient();
        var lifecycleState = OciValues.ParseState<Instance.LifecycleStateEnum>(state);

        var instances = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => compute.ListInstances(
                    new ListInstancesRequest
                    {
                        CompartmentId = compartmentId,
                        LifecycleState = lifecycleState,
                        Page = page
                    },
                    cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);

        if (!String.IsNullOrWhiteSpace(tag))
        {
            var parts = tag.Split('=', 2);
            if (parts.Length == 2)
            {
                instances = instances
                    .Where(x => x.FreeformTags is not null && x.FreeformTags.TryGetValue(parts[0], out var value) && value == parts[1])
                    .ToList();
            }
        }

        var addresses = await ResolveAddressesAsync(compute, network, instances, cancellationToken);

#pragma warning disable IDE0028
        return instances
            .Select(x =>
            {
                var address = addresses.GetValueOrDefault(x.Id);
                return new ComputeInstanceInfo(
                    x.Id,
                    x.CompartmentId,
                    x.DisplayName,
                    OciValues.State(x.LifecycleState),
                    x.Shape,
                    x.ShapeConfig?.Ocpus,
                    x.ShapeConfig?.MemoryInGBs,
                    address?.PublicIp,
                    address?.PrivateIp,
                    x.AvailabilityDomain,
                    OciValues.Tags(x.FreeformTags),
                    x.TimeCreated.GetValueOrDefault());
            })
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Starts an instance, polling until RUNNING when wait is set
    public async ValueTask StartAsync(string instanceId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        await compute.InstanceAction(new InstanceActionRequest { InstanceId = instanceId, Action = "START" }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(compute, instanceId, "RUNNING", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Stops an instance (soft stop unless forced), polling until STOPPED when wait is set
    public async ValueTask StopAsync(string instanceId, bool force, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        await compute.InstanceAction(new InstanceActionRequest { InstanceId = instanceId, Action = force ? "STOP" : "SOFTSTOP" }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(compute, instanceId, "STOPPED", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Reboots an instance (soft reset unless forced)
    public async ValueTask RebootAsync(string instanceId, bool force, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        await compute.InstanceAction(new InstanceActionRequest { InstanceId = instanceId, Action = force ? "RESET" : "SOFTRESET" }, cancellationToken: cancellationToken);
    }

    // Terminates an instance and its boot volume, polling until TERMINATED when wait is set
    public async ValueTask TerminateAsync(string instanceId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        await compute.TerminateInstance(new TerminateInstanceRequest { InstanceId = instanceId, PreserveBootVolume = false }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(compute, instanceId, "TERMINATED", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Runs a shell script through the compute instance agent and waits for the text output
    public async ValueTask<RunCommandResult> RunCommandAsync(string instanceId, string command, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        using var agent = factory.CreateComputeInstanceAgentClient();

        // The command is created in the compartment of the instance
        var instance = await compute.GetInstance(new GetInstanceRequest { InstanceId = instanceId }, cancellationToken: cancellationToken);
        var created = await agent.CreateInstanceAgentCommand(
            new CreateInstanceAgentCommandRequest
            {
                CreateInstanceAgentCommandDetails = new CreateInstanceAgentCommandDetails
                {
                    CompartmentId = instance.Instance.CompartmentId,
                    DisplayName = $"cloudmanager-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ExecutionTimeOutInSeconds = timeoutSeconds,
                    Target = new InstanceAgentCommandTarget { InstanceId = instanceId },
                    Content = new InstanceAgentCommandContent
                    {
                        Source = new InstanceAgentCommandSourceViaTextDetails { Text = command },
                        Output = new InstanceAgentCommandOutputViaTextDetails()
                    }
                }
            },
            cancellationToken: cancellationToken);
        var commandId = created.InstanceAgentCommand.Id;

        InstanceAgentCommandExecution? execution = null;
        await OciPolling.WaitAsync(
            async ct =>
            {
                try
                {
                    var response = await agent.GetInstanceAgentCommandExecution(
                        new GetInstanceAgentCommandExecutionRequest { InstanceAgentCommandId = commandId, InstanceId = instanceId },
                        cancellationToken: ct);
                    execution = response.InstanceAgentCommandExecution;
                    return OciValues.State(execution.LifecycleState);
                }
                catch (OciException ex) when (ex.IsNotFound())
                {
                    // The execution record appears once the agent picks the command up
                    return "PENDING";
                }
            },
            static state => state is "SUCCEEDED" or "FAILED" or "TIMED_OUT" or "CANCELED",
            "completion",
            timeoutSeconds,
            progress,
            cancellationToken);

        var content = execution!.Content as InstanceAgentCommandExecutionOutputViaTextDetails;
        return new RunCommandResult(
            OciValues.State(execution.LifecycleState),
            content?.Text,
            execution.Content?.ExitCode,
            execution.Content?.Message);
    }

    private static ValueTask WaitForStateAsync(ComputeClient compute, string instanceId, string targetState, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken) =>
        OciPolling.WaitForStateAsync(
            async ct =>
            {
                var response = await compute.GetInstance(new GetInstanceRequest { InstanceId = instanceId }, cancellationToken: ct);
                return OciValues.State(response.Instance.LifecycleState);
            },
            targetState,
            timeoutSeconds,
            progress,
            cancellationToken);

    // Looks up the primary VNIC of each instance with bounded parallelism; attachments are listed per compartment
    private static async ValueTask<Dictionary<string, Vnic>> ResolveAddressesAsync(ComputeClient compute, VirtualNetworkClient network, List<Instance> instances, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, Vnic>(StringComparer.Ordinal);
        if (instances.Count == 0)
        {
            return result;
        }

        var attachmentLists = await OciParallel.MapAsync(
            instances.Select(static x => x.CompartmentId).Distinct(StringComparer.Ordinal),
            MaxParallelLookups,
            compartmentId => OciPaging.ListAllAsync(
                page => compute.ListVnicAttachments(new ListVnicAttachmentsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage),
            cancellationToken);
        var instanceIds = instances.Select(static x => x.Id).ToHashSet(StringComparer.Ordinal);
        var targets = attachmentLists
            .SelectMany(static x => x)
            .Where(x => (x.LifecycleState == VnicAttachment.LifecycleStateEnum.Attached) && instanceIds.Contains(x.InstanceId))
            .ToList();

        var vnics = await OciParallel.MapAsync(
            targets,
            MaxParallelLookups,
            async attachment =>
            {
                var response = await network.GetVnic(new GetVnicRequest { VnicId = attachment.VnicId }, cancellationToken: cancellationToken);
                return (attachment.InstanceId, response.Vnic);
            },
            cancellationToken);

        foreach (var (instanceId, vnic) in vnics)
        {
            // Prefer the primary VNIC, fall back to any attached one
            if ((vnic.IsPrimary ?? false) || !result.ContainsKey(instanceId))
            {
                result[instanceId] = vnic;
            }
        }

        return result;
    }
}
