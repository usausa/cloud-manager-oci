namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LoadBalancerPage
{
    private List<LoadBalancerInfo> loadBalancers = [];

    private LoadBalancerInfo? selected;

    private List<BackendSetInfo> backendSets = [];

    private bool isDetailLoading;

    [Inject]
    public required LoadBalancerService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selected = null;
        backendSets = [];
        return LoadAsync(async () =>
        {
            loadBalancers = await Service.ListLoadBalancersAsync(CancellationToken);
        });
    }

    // Load the backend sets of the selected load balancer
    private Task OnSelectedAsync(LoadBalancerInfo? loadBalancer)
    {
        selected = loadBalancer;
        backendSets = [];
        if (loadBalancer is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            backendSets = await Service.ListBackendSetsAsync(loadBalancer.Id, loadBalancer.Type, CancellationToken);
        }, x => isDetailLoading = x);
    }

    private async Task ShowBackendHealthAsync(BackendSetInfo backendSet)
    {
        await DialogService.ShowAsync<BackendHealthDialog>("バックエンドヘルス", new DialogParameters<BackendHealthDialog>
        {
            { x => x.LoadBalancerId, selected!.Id },
            { x => x.LoadBalancerType, selected.Type },
            { x => x.BackendSetName, backendSet.Name }
        },
        Styles.LargeDialog);
    }

    private static Color HealthColor(string status) => status switch
    {
        "OK" => Color.Success,
        "WARNING" => Color.Warning,
        "CRITICAL" => Color.Error,
        _ => Color.Default
    };
}
