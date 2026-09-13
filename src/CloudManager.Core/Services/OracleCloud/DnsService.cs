namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Dns;

using Oci.DnsService.Models;
using Oci.DnsService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class DnsService
{
    private readonly OciClientFactory factory;

    public DnsService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the global and private zones of the compartments in scope
    public async ValueTask<List<DnsZoneInfo>> ListZonesAsync(CancellationToken cancellationToken = default)
    {
        using var dns = factory.CreateDnsClient();
        var result = new List<DnsZoneInfo>();
        foreach (var scope in new[] { Scope.Global, Scope.Private })
        {
            var zones = await factory.ListInScopeAsync(
                dns,
                (client, compartmentId, page) => client.ListZones(new ListZonesRequest { CompartmentId = compartmentId, Scope = scope, Page = page }, cancellationToken: cancellationToken),
                static x => x.Items,
                static x => x.OpcNextPage,
                cancellationToken);
            result.AddRange(zones.Select(static x => new DnsZoneInfo(
                x.Id,
                x.CompartmentId,
                x.Name,
                OciValues.State(x.ZoneType),
                OciValues.State(x.Scope),
                OciValues.State(x.LifecycleState),
                x.Serial,
                x.TimeCreated)));
        }

        result.Sort(static (x, y) => String.Compare(x.Name, y.Name, StringComparison.Ordinal));
        return result;
    }

    // Record sets of a zone grouped by domain and type
    public async ValueTask<List<DnsRecordInfo>> ListRecordsAsync(string zoneId, string scope, CancellationToken cancellationToken = default)
    {
        using var dns = factory.CreateDnsClient();
        var records = await OciPaging.ListAllAsync(
            page => dns.GetZoneRecords(new GetZoneRecordsRequest { ZoneNameOrId = zoneId, Scope = ToScope(scope), Page = page }, cancellationToken: cancellationToken),
            static x => x.RecordCollection.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return records
            .GroupBy(static x => (x.Domain, x.Rtype))
            .Select(static g => new DnsRecordInfo(g.Key.Domain, g.Key.Rtype, g.First().Ttl ?? 0, g.Select(static x => x.Rdata).ToList()))
            .OrderBy(static x => x.Domain, StringComparer.Ordinal)
            .ThenBy(static x => x.Rtype, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Replaces the record set of a domain and type
    public async ValueTask UpsertRecordAsync(string zoneId, string scope, string domain, string rtype, int ttl, IEnumerable<string> values, CancellationToken cancellationToken = default)
    {
        using var dns = factory.CreateDnsClient();
        await dns.UpdateRRSet(
            new UpdateRRSetRequest
            {
                ZoneNameOrId = zoneId,
                Scope = ToScope(scope),
                Domain = domain,
                Rtype = rtype,
                UpdateRRSetDetails = new UpdateRRSetDetails
                {
                    Items = values.Select(x => new RecordDetails { Domain = domain, Rtype = rtype, Ttl = ttl, Rdata = x }).ToList()
                }
            },
            cancellationToken: cancellationToken);
    }

    // Deletes the record set of a domain and type
    public async ValueTask DeleteRecordAsync(string zoneId, string scope, string domain, string rtype, CancellationToken cancellationToken = default)
    {
        using var dns = factory.CreateDnsClient();
        await dns.DeleteRRSet(new DeleteRRSetRequest { ZoneNameOrId = zoneId, Scope = ToScope(scope), Domain = domain, Rtype = rtype }, cancellationToken: cancellationToken);
    }

    // Private zones must be addressed with their scope
    private static Scope? ToScope(string scope) =>
        String.Equals(scope, "PRIVATE", StringComparison.OrdinalIgnoreCase) ? Scope.Private : Scope.Global;
}
#pragma warning restore CA1724
