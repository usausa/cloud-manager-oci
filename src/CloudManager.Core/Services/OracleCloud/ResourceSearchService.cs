namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.ResourceSearch;

using Oci.ResourcesearchService.Models;
using Oci.ResourcesearchService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class ResourceSearchService
{
    // The service stops returning pages after this many results
    public const int MaxResults = 500;

    private readonly OciClientFactory factory;

    public ResourceSearchService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Runs a structured query across the tenancy (paged, capped at MaxResults)
    public async ValueTask<List<ResourceSummaryInfo>> SearchAsync(string query, int limit = MaxResults, CancellationToken cancellationToken = default)
    {
        using var search = factory.CreateResourceSearchClient();
        var result = new List<ResourceSummaryInfo>();
        string? page = null;

        do
        {
            var response = await search.SearchResources(
                new SearchResourcesRequest
                {
                    SearchDetails = new StructuredSearchDetails
                    {
                        Query = query,
                        MatchingContextType = SearchDetails.MatchingContextTypeEnum.None
                    },
                    Limit = Math.Min(limit - result.Count, 1000),
                    Page = page
                },
                cancellationToken: cancellationToken);

            result.AddRange(response.ResourceSummaryCollection.Items.Select(ToInfo));
            page = response.OpcNextPage;
        }
        while (!String.IsNullOrEmpty(page) && (result.Count < limit));

        return result;
    }

    // Counts resources of the given types by state, optionally within the given compartments
    public async ValueTask<List<ResourceStateCount>> CountByStateAsync(IEnumerable<string> resourceTypes, IReadOnlyCollection<string>? compartmentIds, CancellationToken cancellationToken = default)
    {
        var query = $"query {String.Join(", ", resourceTypes)} resources";
        if (compartmentIds is { Count: > 0 })
        {
            query += $" where {String.Join(" || ", compartmentIds.Select(static x => $"compartmentId = '{x}'"))}";
        }

        var resources = await SearchAsync(query, MaxResults, cancellationToken);
#pragma warning disable IDE0028
        return resources
            .GroupBy(static x => (x.ResourceType, State: (x.State ?? string.Empty).ToUpperInvariant()))
            .Select(static g => new ResourceStateCount(g.Key.ResourceType, g.Key.State, g.Count()))
            .OrderBy(static x => x.ResourceType, StringComparer.Ordinal)
            .ThenBy(static x => x.State, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Resource type names usable in queries
    public async ValueTask<List<string>> ListResourceTypesAsync(CancellationToken cancellationToken = default)
    {
        using var search = factory.CreateResourceSearchClient();
        var types = await OciPaging.ListAllAsync(
            page => search.ListResourceTypes(new ListResourceTypesRequest { Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);
#pragma warning disable IDE0028
        return types.Select(static x => x.Name).Order(StringComparer.Ordinal).ToList();
#pragma warning restore IDE0028
    }

    private static ResourceSummaryInfo ToInfo(ResourceSummary x) => new(
        x.ResourceType,
        x.Identifier,
        x.DisplayName,
        x.LifecycleState,
        x.CompartmentId,
        x.AvailabilityDomain,
        x.TimeCreated);
}
#pragma warning restore CA1724
