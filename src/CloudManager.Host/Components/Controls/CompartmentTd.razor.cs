namespace CloudManager.Host.Components.Controls;

using CloudManager.Host.Infrastructure.Components;
using CloudManager.Host.Infrastructure.OracleCloud;

using Microsoft.AspNetCore.Components;

// Cell of the compartment column, shown only when a listing spans several compartments
public sealed partial class CompartmentTd
{
    [Inject]
    public required OciSession Session { get; set; }

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public string? Id { get; set; }

    private string Name => Session.Compartments.FirstOrDefault(x => x.Id == Id)?.Name ?? DisplayFormat.Ocid(Id);

    private string Path => Session.Compartments.FirstOrDefault(x => x.Id == Id)?.Path ?? Id ?? "-";
}
