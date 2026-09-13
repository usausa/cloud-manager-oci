namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class LoggingPage
{
    private List<LogGroupInfo> logGroups = [];

    private List<LogInfo> logs = [];

    private List<LogEntryInfo> entries = [];

    private LogGroupInfo? selectedGroup;

    private LogInfo? selectedLog;

    private bool isLogsLoading;

    private bool searched;

    private string query = string.Empty;

    private int hours = 24;

    private int limit = 100;

    [Inject]
    public required LoggingService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync()
    {
        selectedGroup = null;
        selectedLog = null;
        logs = [];
        ClearResults();
        return LoadAsync(async () =>
        {
            logGroups = await Service.ListLogGroupsAsync(CancellationToken);
        });
    }

    private async Task OnGroupSelectedAsync(LogGroupInfo? group)
    {
        selectedGroup = group;
        selectedLog = null;
        logs = [];
        ClearResults();
        if (group is null)
        {
            return;
        }

        query = Service.BuildQuery(group.Id, null);

        isLogsLoading = true;
        try
        {
            logs = await Service.ListLogsAsync(group.Id, CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = FormatError(ex);
        }
        finally
        {
            isLogsLoading = false;
        }
    }

    // Selecting a log narrows the default query to it
    private void OnLogSelected(LogInfo? log)
    {
        selectedLog = log;
        if (selectedGroup is not null)
        {
            query = Service.BuildQuery(selectedGroup.Id, log?.Id);
        }
    }

    private Task SearchAsync() =>
        RunAsync("検索中...", async (_, cancellationToken) =>
        {
            var end = DateTime.UtcNow;
            entries = await Service.SearchAsync(query.Trim(), end.AddHours(-hours), end, limit, cancellationToken);
            searched = true;
        });

    private async Task ShowEntryAsync(LogEntryInfo entry)
    {
        await DialogService.ShowAsync<LogEntryDialog>("ログエントリ", new DialogParameters<LogEntryDialog>
        {
            { x => x.Entry, entry }
        });
    }

    private void ClearResults()
    {
        entries = [];
        searched = false;
    }
}
