namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class BastionSessionCreateDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string BastionName { get; set; } = string.Empty;

    private string displayName = $"session-{DateTime.Now:yyyyMMddHHmm}";

    private string sessionType = BastionService.SessionTypePortForwarding;

    private string targetResourceId = string.Empty;

    private string targetPrivateIp = string.Empty;

    private int targetPort = 22;

    private string osUserName = "opc";

    private string publicKey = string.Empty;

    private int ttlSeconds = 1800;

    // Managed SSH needs the instance and OS user; port forwarding needs a resource or an IP
    private bool IsValid =>
        !String.IsNullOrWhiteSpace(displayName) &&
        !String.IsNullOrWhiteSpace(publicKey) &&
        (sessionType == BastionService.SessionTypeManagedSsh
            ? !String.IsNullOrWhiteSpace(targetResourceId) && !String.IsNullOrWhiteSpace(osUserName)
            : !String.IsNullOrWhiteSpace(targetResourceId) || !String.IsNullOrWhiteSpace(targetPrivateIp));

    private void Submit() =>
        MudDialog.Close(DialogResult.Ok(new BastionSessionCreateParams(
            displayName.Trim(),
            sessionType,
            targetResourceId.Trim(),
            String.IsNullOrWhiteSpace(targetPrivateIp) ? null : targetPrivateIp.Trim(),
            targetPort,
            sessionType == BastionService.SessionTypeManagedSsh ? osUserName.Trim() : null,
            publicKey.Trim(),
            ttlSeconds)));

    private void Cancel() => MudDialog.Cancel();
}

public sealed record BastionSessionCreateParams(
    string DisplayName,
    string SessionType,
    string TargetResourceId,
    string? TargetPrivateIp,
    int TargetPort,
    string? OsUserName,
    string PublicKey,
    int TtlSeconds);
