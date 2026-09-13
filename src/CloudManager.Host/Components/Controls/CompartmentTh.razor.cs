namespace CloudManager.Host.Components.Controls;

using Microsoft.AspNetCore.Components;

// Header of the compartment column; the page passes whether its listing spans several compartments
// so that the header re-renders once the compartment tree has been loaded
public sealed partial class CompartmentTh
{
    [Parameter]
    public bool Visible { get; set; }
}
