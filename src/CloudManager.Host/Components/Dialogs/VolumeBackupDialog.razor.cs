namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class VolumeBackupDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string VolumeName { get; set; } = string.Empty;

    private string displayName = string.Empty;

    protected override void OnInitialized()
    {
        displayName = $"{VolumeName}-{DateTime.Now:yyyyMMdd-HHmm}";
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(displayName.Trim()));
}
