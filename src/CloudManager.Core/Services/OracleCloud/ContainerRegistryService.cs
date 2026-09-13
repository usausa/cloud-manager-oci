namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.ContainerRegistry;

using Oci.ArtifactsService.Requests;

public sealed class ContainerRegistryService
{
    private readonly OciClientFactory factory;

    public ContainerRegistryService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Registry host of the current region
    public string RegistryHost => $"{factory.Region.RegionCode}.ocir.io";

    // Lists the container repositories of the compartments in scope
    public async ValueTask<List<ContainerRepositoryInfo>> ListRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        using var artifacts = factory.CreateArtifactsClient();
        var repositories = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => artifacts.ListContainerRepositories(new ListContainerRepositoriesRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
                static x => x.ContainerRepositoryCollection.Items,
                static x => x.OpcNextPage),
            cancellationToken);

        var host = RegistryHost;
#pragma warning disable IDE0028
        return repositories
            .Select(x => new ContainerRepositoryInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                $"{host}/{x.Namespace}/{x.DisplayName}",
                OciValues.State(x.LifecycleState),
                x.ImageCount ?? 0,
                x.LayersSizeInBytes ?? 0,
                x.IsPublic ?? false,
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Lists the images of a repository; the name is unique in the tenancy, so the compartments in scope are searched
    public async ValueTask<List<ContainerImageInfo>> ListImagesAsync(string repositoryName, CancellationToken cancellationToken = default)
    {
        using var artifacts = factory.CreateArtifactsClient();
        var images = await factory.ListInScopeAsync(
            compartmentId => OciPaging.ListAllAsync(
                page => artifacts.ListContainerImages(new ListContainerImagesRequest { CompartmentId = compartmentId, RepositoryName = repositoryName, Page = page }, cancellationToken: cancellationToken),
                static x => x.ContainerImageCollection.Items,
                static x => x.OpcNextPage),
            cancellationToken);

#pragma warning disable IDE0028
        return images
            .Select(static x => new ContainerImageInfo(
                x.Id,
                x.DisplayName,
                x.Version,
                x.Digest,
                OciValues.State(x.LifecycleState),
                x.TimeCreated.GetValueOrDefault()))
            .OrderByDescending(static x => x.TimeCreated)
            .ToList();
#pragma warning restore IDE0028
    }

    // Deletes an image by OCID
    public async ValueTask DeleteImageAsync(string imageId, CancellationToken cancellationToken = default)
    {
        using var artifacts = factory.CreateArtifactsClient();
        await artifacts.DeleteContainerImage(new DeleteContainerImageRequest { ImageId = imageId }, cancellationToken: cancellationToken);
    }
}
