namespace CloudManager.Infrastructure.OracleCloud;

using Oci.Common.Model;

public static class OciExceptionExtensions
{
    // Service failures are shown with their service code, everything else with the message
    public static string FormatError(this Exception ex) =>
        ex is OciException oci ? $"[{oci.ServiceCode}] {oci.Message}" : ex.Message;

    public static bool IsNotFound(this OciException ex) =>
        ex.StatusCode == HttpStatusCode.NotFound;
}
