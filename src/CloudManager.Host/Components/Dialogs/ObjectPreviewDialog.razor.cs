namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class ObjectPreviewDialog
{
    private const long MaxImageBytes = 10 * 1024 * 1024;

    private const long MaxTextBytes = 1 * 1024 * 1024;

    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp"];

    private static readonly string[] TextExtensions = [".txt", ".json", ".yaml", ".yml", ".xml", ".log", ".csv", ".md"];

    private string previewType = "none";

    private string? imageData;

    private string? textContent;

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BucketName { get; set; } = string.Empty;

    [Parameter]
    public ObjectInfo? Target { get; set; }

    [Inject]
    public required ObjectStorageService Service { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (Target is null)
        {
            return Task.CompletedTask;
        }

        var ext = Path.GetExtension(Target.Name);
        if (ImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase) && Target.Size <= MaxImageBytes)
        {
            previewType = "image";
        }
        else if (TextExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase) && Target.Size <= MaxTextBytes)
        {
            previewType = "text";
        }
        else
        {
            return Task.CompletedTask;
        }

        return LoadAsync(async () =>
        {
            var bytes = await Service.DownloadBytesAsync(BucketName, Target.Name, null, CancellationToken);
            if (previewType == "image")
            {
                var mime = ext.ToUpperInvariant() switch
                {
                    ".PNG" => "image/png",
                    ".GIF" => "image/gif",
                    ".WEBP" => "image/webp",
                    _ => "image/jpeg"
                };
                imageData = $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
            }
            else
            {
                textContent = Encoding.UTF8.GetString(bytes);
            }
        });
    }
}
