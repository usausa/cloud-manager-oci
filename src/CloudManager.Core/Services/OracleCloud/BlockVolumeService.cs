namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.BlockVolume;

using Oci.CoreService;
using Oci.CoreService.Models;
using Oci.CoreService.Requests;
using Oci.IdentityService.Requests;

public sealed class BlockVolumeService
{
    public const string AttachmentTypeParavirtualized = "PARAVIRTUALIZED";
    public const string AttachmentTypeIscsi = "ISCSI";

    private readonly OciClientFactory factory;

    public BlockVolumeService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists block volumes and boot volumes of the compartments in scope with their attachments
    public async ValueTask<List<BlockVolumeInfo>> ListVolumesAsync(string? state, CancellationToken cancellationToken = default)
    {
        using var blockstorage = factory.CreateBlockstorageClient();
        using var compute = factory.CreateComputeClient();
        using var identity = factory.CreateIdentityClient();

        // Boot volumes are listed per availability domain
        var domains = await identity.ListAvailabilityDomains(new ListAvailabilityDomainsRequest { CompartmentId = factory.TenancyId }, cancellationToken: cancellationToken);
        var domainNames = domains.Items.Select(static x => x.Name).ToList();

        var result = await factory.ListInScopeAsync(
            compartmentId => ListCompartmentVolumesAsync(blockstorage, compute, compartmentId, domainNames, state, cancellationToken),
            cancellationToken);
        result.Sort(static (x, y) => String.Compare(x.DisplayName, y.DisplayName, StringComparison.Ordinal));
        return result;
    }

    private static async ValueTask<List<BlockVolumeInfo>> ListCompartmentVolumesAsync(BlockstorageClient blockstorage, ComputeClient compute, string compartmentId, List<string> domainNames, string? state, CancellationToken cancellationToken)
    {
        var volumes = await OciPaging.ListAllAsync(
            page => blockstorage.ListVolumes(
                new ListVolumesRequest
                {
                    CompartmentId = compartmentId,
                    LifecycleState = OciValues.ParseState<Volume.LifecycleStateEnum>(state),
                    Page = page
                },
                cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);
        var attachments = await OciPaging.ListAllAsync(
            page => compute.ListVolumeAttachments(new ListVolumeAttachmentsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);
        var attachmentByVolume = attachments
            .Where(static x => x.LifecycleState == VolumeAttachment.LifecycleStateEnum.Attached)
            .GroupBy(static x => x.VolumeId, StringComparer.Ordinal)
            .ToDictionary(static g => g.Key, static g => g.First(), StringComparer.Ordinal);

        var result = volumes
            .Select(x =>
            {
                var attachment = attachmentByVolume.GetValueOrDefault(x.Id);
                return new BlockVolumeInfo(
                    x.Id,
                    x.CompartmentId,
                    x.DisplayName,
                    OciValues.State(x.LifecycleState),
                    x.SizeInGBs ?? 0,
                    x.VpusPerGB,
                    x.AvailabilityDomain,
                    false,
                    attachment?.InstanceId,
                    attachment?.Id,
                    x.TimeCreated.GetValueOrDefault());
            })
            .ToList();

        var bootState = OciValues.ParseState<BootVolume.LifecycleStateEnum>(state);
        foreach (var domain in domainNames)
        {
            var bootVolumes = await OciPaging.ListAllAsync(
                page => blockstorage.ListBootVolumes(
                    new ListBootVolumesRequest { AvailabilityDomain = domain, CompartmentId = compartmentId, Page = page },
                    cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage);
            if (bootVolumes.Count == 0)
            {
                continue;
            }

            var bootAttachments = await OciPaging.ListAllAsync(
                page => compute.ListBootVolumeAttachments(
                    new ListBootVolumeAttachmentsRequest { AvailabilityDomain = domain, CompartmentId = compartmentId, Page = page },
                    cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage);
            var bootAttachmentByVolume = bootAttachments
                .Where(static x => x.LifecycleState == BootVolumeAttachment.LifecycleStateEnum.Attached)
                .GroupBy(static x => x.BootVolumeId, StringComparer.Ordinal)
                .ToDictionary(static g => g.Key, static g => g.First(), StringComparer.Ordinal);

            result.AddRange(bootVolumes
                .Where(x => !bootState.HasValue || (x.LifecycleState == bootState))
                .Select(x =>
                {
                    var attachment = bootAttachmentByVolume.GetValueOrDefault(x.Id);
                    return new BlockVolumeInfo(
                        x.Id,
                        x.CompartmentId,
                        x.DisplayName,
                        OciValues.State(x.LifecycleState),
                        x.SizeInGBs ?? 0,
                        x.VpusPerGB,
                        x.AvailabilityDomain,
                        true,
                        attachment?.InstanceId,
                        attachment?.Id,
                        x.TimeCreated.GetValueOrDefault());
                }));
        }

        return result;
    }

    // Creates a backup of a block volume or boot volume
    public async ValueTask<string> CreateBackupAsync(string volumeId, bool isBootVolume, string displayName, CancellationToken cancellationToken = default)
    {
        using var blockstorage = factory.CreateBlockstorageClient();
        if (isBootVolume)
        {
            var response = await blockstorage.CreateBootVolumeBackup(
                new CreateBootVolumeBackupRequest
                {
                    CreateBootVolumeBackupDetails = new CreateBootVolumeBackupDetails { BootVolumeId = volumeId, DisplayName = displayName }
                },
                cancellationToken: cancellationToken);
            return response.BootVolumeBackup.Id;
        }
        else
        {
            var response = await blockstorage.CreateVolumeBackup(
                new CreateVolumeBackupRequest
                {
                    CreateVolumeBackupDetails = new CreateVolumeBackupDetails { VolumeId = volumeId, DisplayName = displayName }
                },
                cancellationToken: cancellationToken);
            return response.VolumeBackup.Id;
        }
    }

    // Attaches a block volume to an instance
    public async ValueTask AttachAsync(string volumeId, string instanceId, string attachmentType, string? device, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        AttachVolumeDetails details = String.Equals(attachmentType, AttachmentTypeIscsi, StringComparison.OrdinalIgnoreCase)
            ? new AttachIScsiVolumeDetails()
            : new AttachParavirtualizedVolumeDetails();
        details.VolumeId = volumeId;
        details.InstanceId = instanceId;
        details.Device = String.IsNullOrWhiteSpace(device) ? null : device;

        await compute.AttachVolume(new AttachVolumeRequest { AttachVolumeDetails = details }, cancellationToken: cancellationToken);
    }

    // Detaches a block volume attachment
    public async ValueTask DetachAsync(string attachmentId, CancellationToken cancellationToken = default)
    {
        using var compute = factory.CreateComputeClient();
        await compute.DetachVolume(new DetachVolumeRequest { VolumeAttachmentId = attachmentId }, cancellationToken: cancellationToken);
    }
}
