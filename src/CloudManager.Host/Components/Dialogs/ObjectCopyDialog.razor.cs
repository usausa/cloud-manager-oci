namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed record ObjectCopyParams(string DestinationBucket, string DestinationName, bool Move);

public sealed partial class ObjectCopyDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string SourceBucket { get; set; } = string.Empty;

    [Parameter]
    public string SourceName { get; set; } = string.Empty;

    private string destinationBucket = string.Empty;

    private string destinationName = string.Empty;

    private bool move;

    private bool IsValid =>
        !String.IsNullOrWhiteSpace(destinationBucket) &&
        !String.IsNullOrWhiteSpace(destinationName) &&
        !(String.Equals(destinationBucket, SourceBucket, StringComparison.Ordinal) && String.Equals(destinationName, SourceName, StringComparison.Ordinal));

    protected override void OnParametersSet()
    {
        destinationBucket = SourceBucket;
        destinationName = SourceName;
    }

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(new ObjectCopyParams(destinationBucket.Trim(), destinationName.Trim(), move)));
}
