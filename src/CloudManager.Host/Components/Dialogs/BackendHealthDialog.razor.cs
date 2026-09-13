namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class BackendHealthDialog
{
    private List<BackendHealthInfo> backends = [];

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string LoadBalancerId { get; set; } = string.Empty;

    [Parameter]
    public string LoadBalancerType { get; set; } = string.Empty;

    [Parameter]
    public string BackendSetName { get; set; } = string.Empty;

    [Inject]
    public required LoadBalancerService Service { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync(async () =>
        {
            backends = await Service.ListBackendHealthAsync(LoadBalancerId, LoadBalancerType, BackendSetName, CancellationToken);
        });

    private static Color HealthColor(string status) => status switch
    {
        "OK" => Color.Success,
        "WARNING" => Color.Warning,
        "CRITICAL" => Color.Error,
        _ => Color.Default
    };

    private static string Flags(BackendHealthInfo backend)
    {
        var flags = new List<string>();
        if (backend.Offline)
        {
            flags.Add("offline");
        }

        if (backend.Drain)
        {
            flags.Add("drain");
        }

        if (backend.Backup)
        {
            flags.Add("backup");
        }

        return flags.Count == 0 ? "-" : String.Join(", ", flags);
    }
}
