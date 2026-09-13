namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.AutonomousDatabase;

using Oci.DatabaseService;
using Oci.DatabaseService.Models;
using Oci.DatabaseService.Requests;
using Oci.WorkrequestsService.Requests;

public sealed class AutonomousDatabaseService
{
    private readonly OciClientFactory factory;

    public AutonomousDatabaseService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the Autonomous Databases of the compartments in scope
    public async ValueTask<List<AutonomousDatabaseInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        var databases = await factory.ListInScopeAsync(
            database,
            (client, compartmentId, page) => client.ListAutonomousDatabases(new ListAutonomousDatabasesRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return databases
            .Select(static x => new AutonomousDatabaseInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                x.DbName,
                OciValues.State(x.LifecycleState),
                OciValues.State(x.DbWorkload),
                x.DbVersion,
                x.ComputeCount,
                OciValues.State(x.ComputeModel),
                x.DataStorageSizeInGBs ?? (x.DataStorageSizeInTBs * 1024),
                x.IsFreeTier ?? false,
                x.IsAutoScalingEnabled ?? false,
                OciValues.State(x.LicenseModel),
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Starts a database, polling until AVAILABLE when wait is set
    public async ValueTask StartAsync(string databaseId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        await database.StartAutonomousDatabase(new StartAutonomousDatabaseRequest { AutonomousDatabaseId = databaseId }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(database, databaseId, "AVAILABLE", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Stops a database, polling until STOPPED when wait is set
    public async ValueTask StopAsync(string databaseId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        await database.StopAutonomousDatabase(new StopAutonomousDatabaseRequest { AutonomousDatabaseId = databaseId }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(database, databaseId, "STOPPED", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Restarts a database, polling until AVAILABLE when wait is set
    public async ValueTask RestartAsync(string databaseId, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        await database.RestartAutonomousDatabase(new RestartAutonomousDatabaseRequest { AutonomousDatabaseId = databaseId }, cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(database, databaseId, "AVAILABLE", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Lists backups of the compartments in scope, or of one database
    public async ValueTask<List<AutonomousDatabaseBackupInfo>> ListBackupsAsync(string? databaseId, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        var backups = String.IsNullOrEmpty(databaseId)
            ? await factory.ListInScopeAsync(compartmentId => ListBackupsAsync(database, compartmentId, null, cancellationToken), cancellationToken)
            : await ListBackupsAsync(database, null, databaseId, cancellationToken);

#pragma warning disable IDE0028
        return backups
            .Select(static x => new AutonomousDatabaseBackupInfo(
                x.Id,
                x.DisplayName,
                x.AutonomousDatabaseId,
                OciValues.State(x.LifecycleState),
                OciValues.State(x.Type),
                x.IsAutomatic ?? false,
                x.IsRestorable ?? false,
                x.TimeStarted,
                x.TimeEnded,
                x.SizeInTBs,
                x.RetentionPeriodInDays))
            .OrderByDescending(static x => x.TimeStarted)
            .ToList();
#pragma warning restore IDE0028
    }

    // Creates a manual backup, polling until ACTIVE when wait is set
    public async ValueTask<string> CreateBackupAsync(string databaseId, string displayName, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        var response = await database.CreateAutonomousDatabaseBackup(
            new CreateAutonomousDatabaseBackupRequest
            {
                CreateAutonomousDatabaseBackupDetails = new CreateAutonomousDatabaseBackupDetails
                {
                    AutonomousDatabaseId = databaseId,
                    DisplayName = displayName
                }
            },
            cancellationToken: cancellationToken);
        var backupId = response.AutonomousDatabaseBackup.Id;

        if (wait)
        {
            await OciPolling.WaitForStateAsync(
                async ct =>
                {
                    var backup = await database.GetAutonomousDatabaseBackup(new GetAutonomousDatabaseBackupRequest { AutonomousDatabaseBackupId = backupId }, cancellationToken: ct);
                    return OciValues.State(backup.AutonomousDatabaseBackup.LifecycleState);
                },
                "ACTIVE",
                timeoutSeconds,
                progress,
                cancellationToken);
        }

        return backupId;
    }

    // Deletes a manual backup
    public async ValueTask DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        await database.DeleteAutonomousDatabaseBackup(new DeleteAutonomousDatabaseBackupRequest { AutonomousDatabaseBackupId = backupId }, cancellationToken: cancellationToken);
    }

    // Restores the database in place to a point in time, polling until AVAILABLE when wait is set
    public async ValueTask RestoreAsync(string databaseId, DateTime timestamp, bool wait, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        await database.RestoreAutonomousDatabase(
            new RestoreAutonomousDatabaseRequest
            {
                AutonomousDatabaseId = databaseId,
                RestoreAutonomousDatabaseDetails = new RestoreAutonomousDatabaseDetails { Timestamp = timestamp }
            },
            cancellationToken: cancellationToken);

        if (wait)
        {
            await WaitForStateAsync(database, databaseId, "AVAILABLE", timeoutSeconds, progress, cancellationToken);
        }
    }

    // Connection strings by service name (high, medium, low, ...)
    public async ValueTask<List<ConnectionStringInfo>> GetConnectionStringsAsync(string databaseId, CancellationToken cancellationToken = default)
    {
        using var database = factory.CreateDatabaseClient();
        var response = await database.GetAutonomousDatabase(new GetAutonomousDatabaseRequest { AutonomousDatabaseId = databaseId }, cancellationToken: cancellationToken);
        var strings = response.AutonomousDatabase.ConnectionStrings?.AllConnectionStrings;
#pragma warning disable IDE0028
        return strings is null
            ? []
            : strings.Select(static x => new ConnectionStringInfo(x.Key, x.Value)).OrderBy(static x => x.Name, StringComparer.Ordinal).ToList();
#pragma warning restore IDE0028
    }

    private static ValueTask<List<AutonomousDatabaseBackupSummary>> ListBackupsAsync(DatabaseClient database, string? compartmentId, string? databaseId, CancellationToken cancellationToken) =>
        OciPaging.ListAllAsync(
            page => database.ListAutonomousDatabaseBackups(
                new ListAutonomousDatabaseBackupsRequest { CompartmentId = compartmentId, AutonomousDatabaseId = databaseId, Page = page },
                cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

    // Work requests of a resource in its compartment, newest first
    public async ValueTask<List<WorkRequestInfo>> ListWorkRequestsAsync(string compartmentId, string resourceId, CancellationToken cancellationToken = default)
    {
        using var workRequest = factory.CreateWorkRequestClient();
        var requests = await OciPaging.ListAllAsync(
            page => workRequest.ListWorkRequests(new ListWorkRequestsRequest { CompartmentId = compartmentId, ResourceId = resourceId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return requests
            .Select(static x => new WorkRequestInfo(
                x.Id,
                x.OperationType,
                OciValues.State(x.Status),
                x.PercentComplete,
                x.TimeAccepted,
                x.TimeStarted,
                x.TimeFinished))
            .OrderByDescending(static x => x.TimeAccepted)
            .ToList();
#pragma warning restore IDE0028
    }

    private static ValueTask WaitForStateAsync(DatabaseClient database, string databaseId, string targetState, int timeoutSeconds, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken) =>
        OciPolling.WaitForStateAsync(
            async ct =>
            {
                var response = await database.GetAutonomousDatabase(new GetAutonomousDatabaseRequest { AutonomousDatabaseId = databaseId }, cancellationToken: ct);
                return OciValues.State(response.AutonomousDatabase.LifecycleState);
            },
            targetState,
            timeoutSeconds,
            progress,
            cancellationToken);
}
