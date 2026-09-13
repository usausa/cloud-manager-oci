namespace CloudManager.Infrastructure.OracleCloud;

// Follows opc-next-page asynchronously (the SDK paginators enumerate synchronously)
public static class OciPaging
{
    public static async ValueTask<List<TItem>> ListAllAsync<TResponse, TItem>(
        Func<string?, Task<TResponse>> fetch,
        Func<TResponse, IEnumerable<TItem>?> items,
        Func<TResponse, string?> nextPage)
    {
        var result = new List<TItem>();
        string? page = null;

        do
        {
            var response = await fetch(page);
            var pageItems = items(response);
            if (pageItems is not null)
            {
                result.AddRange(pageItems);
            }

            page = nextPage(response);
        }
        while (!String.IsNullOrEmpty(page));

        return result;
    }
}
