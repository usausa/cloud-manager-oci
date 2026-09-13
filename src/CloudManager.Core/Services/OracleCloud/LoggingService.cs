namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Oci.LoggingsearchService.Models;
using Oci.LoggingsearchService.Requests;
using Oci.LoggingService.Models;
using Oci.LoggingService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class LoggingService
{
    private readonly OciClientFactory factory;

    public LoggingService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the log groups of the compartments in scope
    public async ValueTask<List<LogGroupInfo>> ListLogGroupsAsync(CancellationToken cancellationToken = default)
    {
        using var logging = factory.CreateLoggingManagementClient();
        var groups = await factory.ListInScopeAsync(
            logging,
            (client, compartmentId, page) => client.ListLogGroups(new ListLogGroupsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return groups
            .Select(static x => new LogGroupInfo(x.Id, x.CompartmentId, x.DisplayName, x.Description, OciValues.State(x.LifecycleState), x.TimeCreated))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Logs of a log group
    public async ValueTask<List<LogInfo>> ListLogsAsync(string logGroupId, CancellationToken cancellationToken = default)
    {
        using var logging = factory.CreateLoggingManagementClient();
        var logs = await OciPaging.ListAllAsync(
            page => logging.ListLogs(new ListLogsRequest { LogGroupId = logGroupId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return logs
            .Select(static x => new LogInfo(
                x.Id,
                x.DisplayName,
                OciValues.State(x.LogType),
                x.IsEnabled ?? false,
                OciValues.State(x.LifecycleState),
                x.RetentionDuration,
                DescribeSource(x.Configuration?.Source)))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Default query for a log group or a single log; the path starts with the compartment of the group
    public static string BuildQuery(string compartmentId, string logGroupId, string? logId) =>
        String.IsNullOrEmpty(logId)
            ? $"search \"{compartmentId}/{logGroupId}\" | sort by datetime desc"
            : $"search \"{compartmentId}/{logGroupId}/{logId}\" | sort by datetime desc";

    // Runs a log search query and returns the entries
    public async ValueTask<List<LogEntryInfo>> SearchAsync(string query, DateTime timeStart, DateTime timeEnd, int limit, CancellationToken cancellationToken = default)
    {
        using var search = factory.CreateLogSearchClient();
        var response = await search.SearchLogs(
            new SearchLogsRequest
            {
                SearchLogsDetails = new SearchLogsDetails
                {
                    SearchQuery = query,
                    TimeStart = timeStart,
                    TimeEnd = timeEnd,
                    IsReturnFieldInfo = false
                },
                Limit = limit
            },
            cancellationToken: cancellationToken);

#pragma warning disable IDE0028
        return (response.SearchResponse.Results ?? []).Select(static x => ToEntry(x.Data)).ToList();
#pragma warning restore IDE0028
    }

    private static string? DescribeSource(Source? source) => source switch
    {
        OciService x => $"{x.Service}/{x.Resource}/{x.Category}",
        null => null,
        _ => source.GetType().Name
    };

    // Entries arrive as JSON objects with datetime and logContent
    private static LogEntryInfo ToEntry(object? data)
    {
        if (data is not JObject json)
        {
            var text = data?.ToString() ?? string.Empty;
            return new LogEntryInfo(null, text, text);
        }

        DateTime? timestamp = null;
        var datetime = json["datetime"];
        if (datetime is not null && datetime.Type != JTokenType.Null)
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(datetime.Value<long>()).UtcDateTime;
        }

        var message = json.SelectToken("logContent.data.message")?.ToString()
                      ?? json.SelectToken("logContent.data")?.ToString(Formatting.None)
                      ?? json.ToString(Formatting.None);
        return new LogEntryInfo(timestamp, message, json.ToString(Formatting.Indented));
    }
}
#pragma warning restore CA1724
