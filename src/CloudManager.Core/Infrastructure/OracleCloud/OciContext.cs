namespace CloudManager.Infrastructure.OracleCloud;

using Oci.Common;
using Oci.Common.Auth;

// Resolved connection context: credentials, region and the compartment to operate on
public sealed record OciContext(
    ConfigFileAuthenticationDetailsProvider Provider,
    Region Region,
    string TenancyId,
    string CompartmentId);
