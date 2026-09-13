namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class ContainerRegistryPage
{
    private List<ContainerRepositoryInfo> repositories = [];

    private string searchText = string.Empty;

    [Inject]
    public required ContainerRegistryService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            repositories = await Service.ListRepositoriesAsync(CancellationToken);
        });

    private bool FilterFunc(ContainerRepositoryInfo r) =>
        String.IsNullOrWhiteSpace(searchText) ||
        r.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
        r.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
