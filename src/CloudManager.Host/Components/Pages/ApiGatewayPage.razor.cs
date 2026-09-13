namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class ApiGatewayPage
{
    private List<ApiGatewayInfo> gateways = [];

    private List<ApiDeploymentInfo> deployments = [];

    private ApiGatewayInfo? selectedGateway;

    private bool isDetailLoading;

    [Inject]
    public required ApiGatewayService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedGateway = null;
        deployments = [];
        return LoadAsync(async () =>
        {
            gateways = await Service.ListGatewaysAsync(CancellationToken);
        });
    }

    private Task OnGatewaySelectedAsync(ApiGatewayInfo? gateway)
    {
        selectedGateway = gateway;
        deployments = [];
        if (gateway is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            deployments = await Service.ListDeploymentsAsync(gateway.Id, CancellationToken);
        }, x => isDetailLoading = x);
    }
}
