namespace CloudManager.Host.Components.Pages;

using Microsoft.AspNetCore.Components;

public sealed partial class VcnPage
{
    private List<VcnInfo> vcns = [];

    private VcnInfo? selectedVcn;

    private IReadOnlyList<SubnetInfo> subnets = [];

    private IReadOnlyList<RouteTableInfo> routeTables = [];

    private IReadOnlyList<SecurityListInfo> securityLists = [];

    private IReadOnlyList<NetworkSecurityGroupInfo> networkSecurityGroups = [];

    private IReadOnlyList<InternetGatewayInfo> internetGateways = [];

    private IReadOnlyList<NatGatewayInfo> natGateways = [];

    private IReadOnlyList<ServiceGatewayInfo> serviceGateways = [];

    private bool isDetailLoading;

    [Inject]
    public required VcnService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedVcn = null;
        ClearDetail();
        return LoadAsync(async () =>
        {
            vcns = await Service.ListVcnsAsync(CancellationToken);
        });
    }

    private void ClearDetail()
    {
        subnets = [];
        routeTables = [];
        securityLists = [];
        networkSecurityGroups = [];
        internetGateways = [];
        natGateways = [];
        serviceGateways = [];
    }

    // Load the components of the selected VCN
    private Task OnVcnSelectedAsync(VcnInfo? vcn)
    {
        selectedVcn = vcn;
        ClearDetail();
        if (vcn is null)
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            var detail = await Service.GetVcnDetailAsync(vcn.Id, CancellationToken);
            subnets = detail.Subnets;
            routeTables = detail.RouteTables;
            securityLists = detail.SecurityLists;
            networkSecurityGroups = detail.NetworkSecurityGroups;
            internetGateways = detail.InternetGateways;
            natGateways = detail.NatGateways;
            serviceGateways = detail.ServiceGateways;
        }, x => isDetailLoading = x);
    }
}
