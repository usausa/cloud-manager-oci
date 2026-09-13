namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.ObjectStorage;

using Oci.Common.Model;
using Oci.ObjectstorageService;
using Oci.ObjectstorageService.Models;
using Oci.ObjectstorageService.Requests;
using Oci.ObjectstorageService.Transfer;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class ObjectStorageService
{
    private const string ObjectFields = "name,size,timeModified,storageTier";

    private const int MaxParallelDeletes = 8;

    private const int WorkRequestTimeoutSeconds = 600;

    private static readonly TimeSpan WorkRequestPollInterval = TimeSpan.FromSeconds(2);

    private readonly OciClientFactory factory;

    public ObjectStorageService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    public ValueTask<string> GetNamespaceAsync(CancellationToken cancellationToken = default) =>
        factory.GetNamespaceAsync(cancellationToken);

    // Lists the buckets of the compartment
    public async ValueTask<List<BucketInfo>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        var buckets = await OciPaging.ListAllAsync(
            page => storage.ListBuckets(new ListBucketsRequest { NamespaceName = namespaceName, CompartmentId = factory.CompartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return buckets
            .Select(static x => new BucketInfo(x.Name, x.Namespace, x.CompartmentId, x.CreatedBy, x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Lists one hierarchy level under the prefix
    public async ValueTask<ObjectListing> ListObjectsWithDelimiterAsync(string bucketName, string prefix, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();

        var prefixes = new SortedSet<string>(StringComparer.Ordinal);
        var objects = new List<ObjectInfo>();
        string? start = null;
        do
        {
            var response = await storage.ListObjects(
                new ListObjectsRequest
                {
                    NamespaceName = namespaceName,
                    BucketName = bucketName,
                    Prefix = String.IsNullOrEmpty(prefix) ? null : prefix,
                    Delimiter = "/",
                    Fields = ObjectFields,
                    Start = start
                },
                cancellationToken: cancellationToken);

            foreach (var p in response.ListObjects.Prefixes ?? [])
            {
                prefixes.Add(p);
            }

            objects.AddRange(response.ListObjects.Objects.Select(ToInfo));
            start = response.ListObjects.NextStartWith;
        }
        while (!String.IsNullOrEmpty(start));

        return new ObjectListing([.. prefixes], objects);
    }

    // Lists every object under the prefix (no hierarchy)
    public async ValueTask<List<ObjectInfo>> ListObjectsAsync(string bucketName, string? prefix, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();

        var objects = new List<ObjectInfo>();
        string? start = null;
        do
        {
            var response = await storage.ListObjects(
                new ListObjectsRequest
                {
                    NamespaceName = namespaceName,
                    BucketName = bucketName,
                    Prefix = String.IsNullOrEmpty(prefix) ? null : prefix,
                    Fields = ObjectFields,
                    Start = start
                },
                cancellationToken: cancellationToken);

            objects.AddRange(response.ListObjects.Objects.Select(ToInfo));
            start = response.ListObjects.NextStartWith;
        }
        while (!String.IsNullOrEmpty(start));

        return objects;
    }

    // Uploads a stream, spooling non-seekable streams to a temporary file for multipart upload
    public async ValueTask UploadStreamAsync(string bucketName, string objectName, Stream stream, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();

        string? tempFile = null;
        try
        {
            var body = stream;
            if (!stream.CanSeek)
            {
                tempFile = Path.GetTempFileName();
                var file = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.DeleteOnClose | FileOptions.Asynchronous);
                await stream.CopyToAsync(file, cancellationToken);
                file.Position = 0;
                body = file;
            }

            await using (body.ConfigureAwait(false))
            {
                var manager = new UploadManager(storage, new UploadConfiguration());
                var request = new UploadManager.UploadRequest(new PutObjectRequest
                {
                    NamespaceName = namespaceName,
                    BucketName = bucketName,
                    ObjectName = objectName,
                    PutObjectBody = body,
                    ContentLength = body.Length
                })
                {
                    AllowOverwrite = true,
                    OnProgress = (completed, total) => progress?.Report(new ProgressUpdate(total > 0 ? (double)completed / total : 0, $"{completed / 1024} KB / {total / 1024} KB"))
                };
                await manager.Upload(request);
            }
        }
        finally
        {
            // DeleteOnClose removes the spool file; guard against early failures
            if ((tempFile is not null) && File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    // Downloads an object into memory, reporting progress
    public async ValueTask<byte[]> DownloadBytesAsync(string bucketName, string objectName, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        var response = await storage.GetObject(
            new GetObjectRequest { NamespaceName = namespaceName, BucketName = bucketName, ObjectName = objectName },
            cancellationToken: cancellationToken,
            completionOption: HttpCompletionOption.ResponseHeadersRead);

        var total = response.ContentLength ?? 0;
        using var memory = new MemoryStream(total > 0 ? (int)Math.Min(total, Int32.MaxValue) : 0);
        var buffer = new byte[81920];
        long transferred = 0;
        int read;
        while ((read = await response.InputStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            transferred += read;
            progress?.Report(new ProgressUpdate(total > 0 ? (double)transferred / total : 0, $"{transferred / 1024} KB / {total / 1024} KB"));
        }

        return memory.ToArray();
    }

    // Streams an object to the destination
    public async ValueTask DownloadToStreamAsync(string bucketName, string objectName, Stream destination, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        var response = await storage.GetObject(
            new GetObjectRequest { NamespaceName = namespaceName, BucketName = bucketName, ObjectName = objectName },
            cancellationToken: cancellationToken,
            completionOption: HttpCompletionOption.ResponseHeadersRead);
        await response.InputStream.CopyToAsync(destination, cancellationToken);
    }

    // Returns null when the object does not exist
    public async ValueTask<ObjectHead?> HeadObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        try
        {
            var response = await storage.HeadObject(
                new HeadObjectRequest { NamespaceName = namespaceName, BucketName = bucketName, ObjectName = objectName },
                cancellationToken: cancellationToken);
            return new ObjectHead(response.ContentLength ?? 0, response.ContentType ?? "application/octet-stream");
        }
        catch (OciException ex) when (ex.IsNotFound())
        {
            return null;
        }
    }

    public async ValueTask DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        await storage.DeleteObject(new DeleteObjectRequest { NamespaceName = namespaceName, BucketName = bucketName, ObjectName = objectName }, cancellationToken: cancellationToken);
    }

    // Deletes objects with bounded parallelism
    public async ValueTask DeleteObjectsAsync(string bucketName, IEnumerable<string> objectNames, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();

        var names = objectNames.ToList();
        var deleted = 0;
        using var semaphore = new SemaphoreSlim(MaxParallelDeletes);
        await Task.WhenAll(names.Select(async name =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await storage.DeleteObject(new DeleteObjectRequest { NamespaceName = namespaceName, BucketName = bucketName, ObjectName = name }, cancellationToken: cancellationToken);
                var count = Interlocked.Increment(ref deleted);
                progress?.Report(new ProgressUpdate((double)count / names.Count, $"{count} / {names.Count}"));
            }
            finally
            {
                semaphore.Release();
            }
        }));
    }

    // Deletes every object under the prefix
    public async ValueTask DeleteObjectsByPrefixAsync(string bucketName, string prefix, IProgress<ProgressUpdate>? progress, CancellationToken cancellationToken = default)
    {
        var objects = await ListObjectsAsync(bucketName, prefix, cancellationToken);
        await DeleteObjectsAsync(bucketName, objects.Select(static x => x.Name), progress, cancellationToken);
    }

    // Copies an object within the region and waits for the work request to finish
    public async ValueTask CopyObjectAsync(string sourceBucket, string sourceName, string destinationBucket, string destinationName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        await CopyAsync(storage, namespaceName, sourceBucket, sourceName, null, destinationBucket, destinationName, cancellationToken);
    }

    // Moves an object, renaming within the same bucket and copying then deleting across buckets
    public async ValueTask MoveObjectAsync(string sourceBucket, string sourceName, string destinationBucket, string destinationName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();

        if (String.Equals(sourceBucket, destinationBucket, StringComparison.Ordinal))
        {
            await storage.RenameObject(
                new RenameObjectRequest
                {
                    NamespaceName = namespaceName,
                    BucketName = sourceBucket,
                    RenameObjectDetails = new RenameObjectDetails { SourceName = sourceName, NewName = destinationName }
                },
                cancellationToken: cancellationToken);
            return;
        }

        await CopyAsync(storage, namespaceName, sourceBucket, sourceName, null, destinationBucket, destinationName, cancellationToken);
        await storage.DeleteObject(new DeleteObjectRequest { NamespaceName = namespaceName, BucketName = sourceBucket, ObjectName = sourceName }, cancellationToken: cancellationToken);
    }

    // Versions of one object, newest first
    public async ValueTask<List<ObjectVersionInfo>> ListVersionsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        var versions = await OciPaging.ListAllAsync(
            page => storage.ListObjectVersions(
                new ListObjectVersionsRequest { NamespaceName = namespaceName, BucketName = bucketName, Prefix = objectName, Fields = ObjectFields, Page = page },
                cancellationToken: cancellationToken),
            static x => x.ObjectVersionCollection.Items,
            static x => x.OpcNextPage);

        var ordered = versions
            .Where(x => x.Name == objectName)
            .OrderByDescending(static x => x.TimeModified)
            .ToList();
        var latest = ordered.FirstOrDefault(static x => !(x.IsDeleteMarker ?? false));
#pragma warning disable IDE0028
        return ordered
            .Select(x => new ObjectVersionInfo(x.VersionId, ReferenceEquals(x, latest), x.TimeModified, x.Size ?? 0, x.IsDeleteMarker ?? false))
            .ToList();
#pragma warning restore IDE0028
    }

    // Restores a version by copying it over the current object
    public async ValueTask RestoreVersionAsync(string bucketName, string objectName, string versionId, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        await CopyAsync(storage, namespaceName, bucketName, objectName, versionId, bucketName, objectName, cancellationToken);
    }

    // Lifecycle rules of a bucket, empty when no policy is set
    public async ValueTask<List<ObjectLifecycleRuleInfo>> GetLifecycleAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        try
        {
            var response = await storage.GetObjectLifecyclePolicy(new GetObjectLifecyclePolicyRequest { NamespaceName = namespaceName, BucketName = bucketName }, cancellationToken: cancellationToken);
#pragma warning disable IDE0028
            return (response.ObjectLifecyclePolicy.Items ?? [])
                .Select(static x => new ObjectLifecycleRuleInfo(
                    x.Name,
                    x.Action,
                    x.Target ?? "objects",
                    $"{x.TimeAmount} {OciValues.State(x.TimeUnit)}",
                    x.IsEnabled ?? false,
                    x.ObjectNameFilter?.InclusionPrefixes ?? []))
                .ToList();
#pragma warning restore IDE0028
        }
        catch (OciException ex) when (ex.IsNotFound())
        {
            return [];
        }
    }

    // Visibility and settings of a bucket
    public async ValueTask<BucketAccessInfo> GetBucketAccessAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        var namespaceName = await factory.GetNamespaceAsync(cancellationToken);
        using var storage = factory.CreateObjectStorageClient();
        var response = await storage.GetBucket(
            new GetBucketRequest
            {
                NamespaceName = namespaceName,
                BucketName = bucketName,
                Fields = [GetBucketRequest.FieldsEnum.ApproximateCount, GetBucketRequest.FieldsEnum.ApproximateSize]
            },
            cancellationToken: cancellationToken);
        var bucket = response.Bucket;
        var isPublic = bucket.PublicAccessType is Bucket.PublicAccessTypeEnum.ObjectRead or Bucket.PublicAccessTypeEnum.ObjectReadWithoutList;

        return new BucketAccessInfo(
            bucket.Name,
            OciValues.State(bucket.PublicAccessType),
            OciValues.State(bucket.Versioning),
            OciValues.State(bucket.StorageTier),
            bucket.AutoTiering == Bucket.AutoTieringEnum.InfrequentAccess,
            bucket.ApproximateCount,
            bucket.ApproximateSize,
            isPublic ? "パブリック (匿名読み取り可)" : "プライベート");
    }

    private static ObjectInfo ToInfo(ObjectSummary x) =>
        new(x.Name, x.Size ?? 0, x.TimeModified, OciValues.State(x.StorageTier));

    private async ValueTask CopyAsync(ObjectStorageClient storage, string namespaceName, string sourceBucket, string sourceName, string? sourceVersionId, string destinationBucket, string destinationName, CancellationToken cancellationToken)
    {
        var response = await storage.CopyObject(
            new CopyObjectRequest
            {
                NamespaceName = namespaceName,
                BucketName = sourceBucket,
                CopyObjectDetails = new CopyObjectDetails
                {
                    SourceObjectName = sourceName,
                    SourceVersionId = sourceVersionId,
                    DestinationRegion = factory.Region.RegionId,
                    DestinationNamespace = namespaceName,
                    DestinationBucket = destinationBucket,
                    DestinationObjectName = destinationName
                }
            },
            cancellationToken: cancellationToken);

        await WaitForWorkRequestAsync(storage, response.OpcWorkRequestId, cancellationToken);
    }

    // Copy is asynchronous; wait until the work request completes
    private static async ValueTask WaitForWorkRequestAsync(ObjectStorageClient storage, string workRequestId, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(WorkRequestTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(WorkRequestPollInterval, cancellationToken);

            var response = await storage.GetWorkRequest(new GetWorkRequestRequest { WorkRequestId = workRequestId }, cancellationToken: cancellationToken);
            switch (response.WorkRequest.Status)
            {
                case WorkRequest.StatusEnum.Completed:
                    return;
                case WorkRequest.StatusEnum.Failed:
                case WorkRequest.StatusEnum.Canceled:
                    throw new InvalidOperationException($"Copy work request {OciValues.State(response.WorkRequest.Status)}. id=[{workRequestId}]");
            }
        }

        throw new TimeoutException($"Copy work request did not complete within {WorkRequestTimeoutSeconds} seconds.");
    }
}
#pragma warning restore CA1724
