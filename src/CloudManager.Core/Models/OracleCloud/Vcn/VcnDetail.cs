namespace CloudManager.Models.OracleCloud.Vcn;

public sealed record VcnDetail(
    VcnInfo Vcn,
    IReadOnlyList<SubnetInfo> Subnets,
    IReadOnlyList<RouteTableInfo> RouteTables,
    IReadOnlyList<SecurityListInfo> SecurityLists,
    IReadOnlyList<NetworkSecurityGroupInfo> NetworkSecurityGroups,
    IReadOnlyList<InternetGatewayInfo> InternetGateways,
    IReadOnlyList<NatGatewayInfo> NatGateways,
    IReadOnlyList<ServiceGatewayInfo> ServiceGateways);
