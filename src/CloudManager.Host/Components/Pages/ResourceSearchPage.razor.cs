namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class ResourceSearchPage
{
    private const string AllTypes = "all";

    private List<string> resourceTypes = [];

    private List<ResourceSummaryInfo> results = [];

    private string resourceType = AllTypes;

    private string query = string.Empty;

    private string filterText = string.Empty;

    private bool compartmentOnly;

    private bool searched;

    [Inject]
    public required ResourceSearchService Service { get; set; }

    private bool IsTenancyScope => String.Equals(Session.CompartmentId, Session.TenancyId, StringComparison.Ordinal);

    protected override Task OnInitializedAsync()
    {
        compartmentOnly = !IsTenancyScope;
        query = BuildQuery();
        return LoadAsync(async () =>
        {
            resourceTypes = await Service.ListResourceTypesAsync(CancellationToken);
        });
    }

    protected override Task OnSessionChangedAsync()
    {
        compartmentOnly = !IsTenancyScope;
        query = BuildQuery();
        results = [];
        searched = false;
        return Task.CompletedTask;
    }

    private Task SearchAsync() =>
        LoadAsync(async () =>
        {
            results = await Service.SearchAsync(query.Trim(), ResourceSearchService.MaxResults, CancellationToken);
            searched = true;
        });

    private void OnResourceTypeChanged(string? value)
    {
        resourceType = value ?? AllTypes;
        query = BuildQuery();
    }

    private void OnScopeChanged(bool value)
    {
        compartmentOnly = value;
        query = BuildQuery();
    }

    private bool FilterFunc(ResourceSummaryInfo resource) =>
        String.IsNullOrWhiteSpace(filterText) ||
        (resource.DisplayName ?? string.Empty).Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
        resource.ResourceType.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
        resource.Identifier.Contains(filterText, StringComparison.OrdinalIgnoreCase);

    // The compartment condition matches the compartment itself, not its children
    private string BuildQuery()
    {
        var text = $"query {resourceType} resources";
        return compartmentOnly ? $"{text} where compartmentId = '{Session.CompartmentId}'" : text;
    }
}
