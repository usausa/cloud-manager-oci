namespace CloudManager.Models.OracleCloud.LoadBalancer;

public sealed record BackendHealthInfo(
    string Name,
    string IpAddress,
    int Port,
    string Status,
    int Weight,
    bool Offline,
    bool Drain,
    bool Backup);
