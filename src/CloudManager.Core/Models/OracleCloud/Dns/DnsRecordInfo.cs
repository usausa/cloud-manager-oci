namespace CloudManager.Models.OracleCloud.Dns;

// Records with the same domain and type form one record set
public sealed record DnsRecordInfo(
    string Domain,
    string Rtype,
    int Ttl,
    IReadOnlyList<string> Rdata);
