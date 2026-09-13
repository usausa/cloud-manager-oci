namespace CloudManager.Host.Endpoints;

using CloudManager.Host.Infrastructure.Filters;
using CloudManager.Infrastructure.OracleCloud;

public static class ObjectStorageEndpoints
{
    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    public static void MapObjectStorageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(ApiRoutes.ObjectStorage)
            .AddEndpointFilter<OciExceptionFilter>();

        group.MapGet("/download/{bucket}", HandleDownloadAsync);
    }

    //--------------------------------------------------------------------------------
    // Handler
    //--------------------------------------------------------------------------------

    // Direct browser download; profile and region are taken from the query
    private static async ValueTask<IResult> HandleDownloadAsync(
        string bucket,
        string? name,
        string? profile,
        string? region,
        CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(name) || String.IsNullOrEmpty(profile))
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "name and profile are required.");
        }

        // Objects are addressed by bucket and name, so no compartment is needed
        var service = new ObjectStorageService(OciClientFactory.Create(profile, region, null));

        var head = await service.HeadObjectAsync(bucket, name, cancellationToken);
        if (head is null)
        {
            return TypedResults.NotFound();
        }

        var fileName = name.Split('/').LastOrDefault(static x => x.Length > 0) ?? name;
        return TypedResults.Stream(
            stream => service.DownloadToStreamAsync(bucket, name, stream, cancellationToken).AsTask(),
            head.ContentType,
            fileName);
    }
}
