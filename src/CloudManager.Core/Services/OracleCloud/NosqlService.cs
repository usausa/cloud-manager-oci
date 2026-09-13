namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Nosql;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Oci.NosqlService.Models;
using Oci.NosqlService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class NosqlService
{
    private readonly OciClientFactory factory;

    public NosqlService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the tables of the compartment
    public async ValueTask<List<NosqlTableInfo>> ListTablesAsync(CancellationToken cancellationToken = default)
    {
        using var nosql = factory.CreateNosqlClient();
        var tables = await OciPaging.ListAllAsync(
            page => nosql.ListTables(new ListTablesRequest { CompartmentId = factory.CompartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.TableCollection.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return tables
            .Select(static x => new NosqlTableInfo(
                x.Id,
                x.Name,
                OciValues.State(x.LifecycleState),
                OciValues.State(x.TableLimits?.CapacityMode),
                x.TableLimits?.MaxReadUnits,
                x.TableLimits?.MaxWriteUnits,
                x.TableLimits?.MaxStorageInGBs,
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.Name, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Schema of a table
    public async ValueTask<NosqlTableDetail> GetTableAsync(string tableNameOrId, CancellationToken cancellationToken = default)
    {
        using var nosql = factory.CreateNosqlClient();
        var response = await nosql.GetTable(new GetTableRequest { TableNameOrId = tableNameOrId, CompartmentId = factory.CompartmentId }, cancellationToken: cancellationToken);
        var table = response.Table;
#pragma warning disable IDE0028
        return new NosqlTableDetail(
            table.Name,
            table.DdlStatement,
            table.Schema?.Ttl,
            (table.Schema?.Columns ?? []).Select(static x => new NosqlColumnInfo(x.Name, x.Type, x.IsNullable ?? true)).ToList(),
            table.Schema?.PrimaryKey ?? []);
#pragma warning restore IDE0028
    }

    // Runs a SQL statement (e.g. SELECT * FROM t) and returns up to limit rows
    public async ValueTask<List<NosqlRowInfo>> QueryAsync(string statement, int limit, CancellationToken cancellationToken = default)
    {
        using var nosql = factory.CreateNosqlClient();
        var rows = new List<NosqlRowInfo>();
        string? page = null;
        do
        {
            var response = await nosql.Query(
                new QueryRequest
                {
                    QueryDetails = new QueryDetails { CompartmentId = factory.CompartmentId, Statement = statement },
                    Limit = Math.Min(limit - rows.Count, 1000),
                    Page = page
                },
                cancellationToken: cancellationToken);

            rows.AddRange(response.QueryResultCollection.Items.Select(ToRow));
            page = response.OpcNextPage;
        }
        while (!String.IsNullOrEmpty(page) && (rows.Count < limit));

        return rows;
    }

    private static NosqlRowInfo ToRow(Dictionary<string, object> item) =>
        new(item.ToDictionary(static x => x.Key, static x => Format(x.Value), StringComparer.Ordinal));

    // Nested values arrive as JSON tokens
    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        JToken token => token.ToString(Formatting.None),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
    };
}
#pragma warning restore CA1724
