namespace CloudManager.Infrastructure.OracleCloud;

using Oci.Common;
using Oci.Common.Auth;

// Resolved connection context: credentials, region, the selected compartment and the compartments listed for it
// (the selected compartment and its descendants; the tenancy root covers the whole tenancy)
public sealed record OciContext(
    ConfigFileAuthenticationDetailsProvider Provider,
    Region Region,
    string TenancyId,
    string CompartmentId,
    IReadOnlyList<string> CompartmentIds);
