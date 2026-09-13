namespace CloudManager.Models.OracleCloud.Bastion;

// SshCommand is the ready-to-use command provided by the service once the session is active
public sealed record BastionSessionDetail(
    BastionSessionInfo Session,
    string? BastionUserName,
    string? SshCommand);
