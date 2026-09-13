namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed record VolumeAttachParams(string InstanceId, string AttachmentType, string? Device);

public sealed partial class VolumeAttachDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string VolumeName { get; set; } = string.Empty;

    private string instanceId = string.Empty;

    private string attachmentType = BlockVolumeService.AttachmentTypeParavirtualized;

    private string device = string.Empty;

    private bool IsValid => instanceId.StartsWith("ocid1.instance.", StringComparison.Ordinal);

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(new VolumeAttachParams(instanceId.Trim(), attachmentType, String.IsNullOrWhiteSpace(device) ? null : device.Trim())));
}
