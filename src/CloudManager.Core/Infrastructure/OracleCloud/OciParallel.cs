namespace CloudManager.Infrastructure.OracleCloud;

// Runs lookups with bounded parallelism, keeping the source order
public static class OciParallel
{
    public static async Task<List<TResult>> MapAsync<TSource, TResult>(
        IEnumerable<TSource> source,
        int maxParallel,
        Func<TSource, ValueTask<TResult>> map,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(maxParallel);
        var results = await Task.WhenAll(source.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await map(item);
            }
            finally
            {
                semaphore.Release();
            }
        }));
        return [.. results];
    }
}
