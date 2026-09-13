namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.PublicIp;

using Oci.CoreService.Models;
using Oci.CoreService.Requests;

public sealed class PublicIpService
{
    private readonly OciClientFactory factory;

    public PublicIpService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the reserved public IPs of the compartment
    public async ValueTask<List<PublicIpInfo>> ListPublicIpsAsync(CancellationToken cancellationToken = default)
    {
        using var network = factory.CreateVirtualNetworkClient();
        var publicIps = await OciPaging.ListAllAsync(
            page => network.ListPublicIps(
                new ListPublicIpsRequest
                {
                    CompartmentId = factory.CompartmentId,
                    Scope = ListPublicIpsRequest.ScopeEnum.Region,
                    Lifetime = ListPublicIpsRequest.LifetimeEnum.Reserved,
                    Page = page
                },
                cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return publicIps
            .Select(static x => new PublicIpInfo(
                x.Id,
                x.DisplayName,
                x.IpAddress,
                OciValues.State(x.LifecycleState),
                OciValues.State(x.Lifetime),
                x.AssignedEntityId,
                OciValues.State(x.AssignedEntityType),
                x.PrivateIpId,
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Assigns a reserved public IP to the primary private IP of an instance
    public async ValueTask AssignAsync(string publicIpId, string instanceId, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        using var network = factory.CreateVirtualNetworkClient();

        var attachments = await OciPaging.ListAllAsync(
            page => compute.ListVnicAttachments(new ListVnicAttachmentsRequest { CompartmentId = factory.CompartmentId, InstanceId = instanceId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

        string? privateIpId = null;
        foreach (var attachment in attachments.Where(static x => x.LifecycleState == VnicAttachment.LifecycleStateEnum.Attached))
        {
            var vnic = await network.GetVnic(new GetVnicRequest { VnicId = attachment.VnicId }, cancellationToken: cancellationToken);
            if (!(vnic.Vnic.IsPrimary ?? false))
            {
                continue;
            }

            var privateIps = await network.ListPrivateIps(new ListPrivateIpsRequest { VnicId = attachment.VnicId }, cancellationToken: cancellationToken);
            privateIpId = privateIps.Items.FirstOrDefault(static x => x.IsPrimary ?? false)?.Id;
            break;
        }

        if (privateIpId is null)
        {
            throw new InvalidOperationException($"Primary private IP not found. instance=[{instanceId}]");
        }

        await network.UpdatePublicIp(
            new UpdatePublicIpRequest { PublicIpId = publicIpId, UpdatePublicIpDetails = new UpdatePublicIpDetails { PrivateIpId = privateIpId } },
            cancellationToken: cancellationToken);
    }

    // Unassigns a reserved public IP (an empty private IP id releases the assignment)
    public async ValueTask UnassignAsync(string publicIpId, CancellationToken cancellationToken = default)
    {
        using var network = factory.CreateVirtualNetworkClient();
        await network.UpdatePublicIp(
            new UpdatePublicIpRequest { PublicIpId = publicIpId, UpdatePublicIpDetails = new UpdatePublicIpDetails { PrivateIpId = string.Empty } },
            cancellationToken: cancellationToken);
    }
}
