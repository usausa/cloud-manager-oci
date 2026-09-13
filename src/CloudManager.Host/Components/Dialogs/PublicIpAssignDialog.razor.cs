namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class PublicIpAssignDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string IpAddress { get; set; } = string.Empty;

    private string instanceId = string.Empty;

    private bool IsValid => instanceId.Trim().StartsWith("ocid1.instance.", StringComparison.Ordinal);

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(instanceId.Trim()));
}
