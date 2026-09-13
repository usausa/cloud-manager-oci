namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using MudBlazor;

public sealed partial class ObjectUploadDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    // The current prefix is prepended to the file name
    [Parameter]
    public string Prefix { get; set; } = string.Empty;

    private IBrowserFile? selectedFile;

    private string name = string.Empty;

    private void OnFileChanged(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        if (String.IsNullOrWhiteSpace(name))
        {
            name = Prefix + e.File.Name;
        }
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(new ObjectUploadParams(selectedFile!, name)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record ObjectUploadParams(IBrowserFile File, string Name);
