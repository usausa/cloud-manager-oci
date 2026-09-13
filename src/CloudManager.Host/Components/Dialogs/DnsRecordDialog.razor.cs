namespace CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class DnsRecordDialog
{
    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    [Parameter]
    public string ZoneName { get; set; } = string.Empty;

    // When set, the dialog edits the existing record set
    [Parameter]
    public DnsRecordInfo? Record { get; set; }

    private string domain = string.Empty;

    private string rtype = "A";

    private int ttl = 300;

    private string valuesText = string.Empty;

    protected override void OnParametersSet()
    {
        if (Record is null)
        {
            return;
        }

        domain = Record.Domain;
        rtype = Record.Rtype;
        ttl = Record.Ttl;
        valuesText = String.Join('\n', Record.Rdata);
    }

    private void Submit()
    {
        var values = valuesText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        MudDialog.Close(DialogResult.Ok(new DnsRecordParams(domain.Trim(), rtype, ttl, values)));
    }

    private void Cancel() => MudDialog.Cancel();
}

public sealed record DnsRecordParams(string Domain, string Rtype, int Ttl, IReadOnlyList<string> Values);
