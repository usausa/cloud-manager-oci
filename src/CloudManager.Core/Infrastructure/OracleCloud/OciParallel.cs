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
        var items = source.ToList();
        var results = new TResult[items.Count];
        var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(Enumerable.Range(0, items.Count), options, async (index, _) =>
        {
            results[index] = await map(items[index]);
        });
        return [.. results];
    }
}
