namespace CloudManager.Models.OracleCloud.ObjectStorage;

public sealed record ObjectHead(
    long ContentLength,
    string ContentType);
