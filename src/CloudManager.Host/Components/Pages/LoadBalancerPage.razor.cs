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
    private async Task OnSelectedAsync(LoadBalancerInfo? loadBalancer)
    {
        selected = loadBalancer;
        backendSets = [];
        if (loadBalancer is null)
        {
            return;
        }

        isDetailLoading = true;
        try
        {
            backendSets = await Service.ListBackendSetsAsync(loadBalancer.Id, loadBalancer.Type, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isDetailLoading = false;
        }
    }

    private async Task ShowBackendHealthAsync(BackendSetInfo backendSet)
    {
        await DialogService.ShowAsync<BackendHealthDialog>("バックエンドヘルス", new DialogParameters<BackendHealthDialog>
        {
            { x => x.LoadBalancerId, selected!.Id },
            { x => x.LoadBalancerType, selected.Type },
            { x => x.BackendSetName, backendSet.Name }
        });
    }

    private static Color HealthColor(string status) => status switch
    {
        "OK" => Color.Success,
        "WARNING" => Color.Warning,
        "CRITICAL" => Color.Error,
        _ => Color.Default
    };
}
