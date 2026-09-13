namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class AutonomousDatabasePage
{
    private const int WaitTimeoutSeconds = 900;

    private List<AutonomousDatabaseInfo> databases = [];

    private List<AutonomousDatabaseBackupInfo> backups = [];

    private bool isBackupLoading;

    [Inject]
    public required AutonomousDatabaseService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            databases = await Service.ListAsync(CancellationToken);
            await LoadBackupsCoreAsync();
        });

    private Task LoadBackupsAsync() => LoadAsync(LoadBackupsCoreAsync);

    private async Task LoadBackupsCoreAsync()
    {
        isBackupLoading = true;
        try
        {
            backups = await Service.ListBackupsAsync(null, CancellationToken);
        }
        finally
        {
            isBackupLoading = false;
        }
    }

    private string DatabaseName(string databaseId) =>
        databases.FirstOrDefault(x => x.Id == databaseId)?.DisplayName ?? DisplayFormat.Ocid(databaseId);

    private async Task StartAsync(AutonomousDatabaseInfo database)
    {
        if (await DialogService.ShowOperationConfirm("起動", $"データベース {database.DisplayName} を起動しますか？") is null)
        {
            return;
        }

        await RunAsync("起動中...", async (progress, cancellationToken) =>
        {
            await Service.StartAsync(database.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{database.DisplayName} を起動しました。");
        }, LoadAsync);
    }

    private async Task StopAsync(AutonomousDatabaseInfo database)
    {
        if (await DialogService.ShowOperationConfirm("停止", $"データベース {database.DisplayName} を停止しますか？") is null)
        {
            return;
        }

        await RunAsync("停止中...", async (progress, cancellationToken) =>
        {
            await Service.StopAsync(database.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{database.DisplayName} を停止しました。");
        }, LoadAsync);
    }

    private async Task RestartAsync(AutonomousDatabaseInfo database)
    {
        if (await DialogService.ShowOperationConfirm("再起動", $"データベース {database.DisplayName} を再起動しますか？接続中のセッションは切断されます。") is null)
        {
            return;
        }

        await RunAsync("再起動中...", async (progress, cancellationToken) =>
        {
            await Service.RestartAsync(database.Id, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{database.DisplayName} を再起動しました。");
        }, LoadAsync);
    }

    private async Task CreateBackupAsync(AutonomousDatabaseInfo database)
    {
        var dialog = await DialogService.ShowAsync<AdbBackupCreateDialog>("バックアップ作成", new DialogParameters<AdbBackupCreateDialog>
        {
            { x => x.DatabaseName, database.DisplayName }
        });
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var displayName = (string)dialogResult.Data!;
        await RunAsync("バックアップ作成中...", async (progress, cancellationToken) =>
        {
            await Service.CreateBackupAsync(database.Id, displayName, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{displayName} を作成しました。");
        }, LoadAsync);
    }

    // Restores the database in place to the end time of the backup
    private async Task RestoreAsync(AutonomousDatabaseBackupInfo backup)
    {
        var name = DatabaseName(backup.DatabaseId);
        var timestamp = backup.TimeEnded!.Value;
        var message = $"データベース {name} を {DisplayFormat.Time(timestamp)} 時点へ復元(上書き)します。現在のデータは失われます。確認のためデータベース名を入力してください。";
        if (await DialogService.ShowOperationConfirm("復元", message, requireConfirmText: name) is null)
        {
            return;
        }

        await RunAsync("復元中...", async (progress, cancellationToken) =>
        {
            await Service.RestoreAsync(backup.DatabaseId, timestamp, wait: true, WaitTimeoutSeconds, progress, cancellationToken);
            Snackbar.AddSuccess($"{name} を復元しました。");
        }, LoadAsync);
    }

    private async Task DeleteBackupAsync(AutonomousDatabaseBackupInfo backup)
    {
        if (await DialogService.ShowOperationConfirm("バックアップ削除", $"バックアップ {backup.DisplayName} を削除しますか？", requireConfirmText: backup.DisplayName) is null)
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            await Service.DeleteBackupAsync(backup.Id, cancellationToken);
            Snackbar.AddSuccess($"{backup.DisplayName} を削除しました。");
        }, LoadBackupsAsync);
    }

    private async Task ShowConnectionStringsAsync(AutonomousDatabaseInfo database)
    {
        await DialogService.ShowAsync<AdbConnectionDialog>("接続文字列", new DialogParameters<AdbConnectionDialog>
        {
            { x => x.DatabaseId, database.Id },
            { x => x.DatabaseName, database.DisplayName }
        },
        Styles.MediumDialog);
    }

    private async Task ShowWorkRequestsAsync(AutonomousDatabaseInfo database)
    {
        await DialogService.ShowAsync<WorkRequestsDialog>("作業リクエスト", new DialogParameters<WorkRequestsDialog>
        {
            { x => x.CompartmentId, database.CompartmentId },
            { x => x.ResourceId, database.Id },
            { x => x.ResourceName, database.DisplayName }
        },
        Styles.MediumDialog);
    }

    private static Color StateColor(string state) => state switch
    {
        "AVAILABLE" => Color.Success,
        "STOPPED" => Color.Default,
        "STARTING" or "STOPPING" or "RESTARTING" or "PROVISIONING" or "UPDATING" or "SCALE_IN_PROGRESS" or "BACKUP_IN_PROGRESS" or "RESTORE_IN_PROGRESS" or "MAINTENANCE_IN_PROGRESS" => Color.Warning,
        "TERMINATING" or "TERMINATED" or "UNAVAILABLE" or "RESTORE_FAILED" or "AVAILABLE_NEEDS_ATTENTION" or "INACCESSIBLE" => Color.Error,
        _ => Color.Default
    };

    private static Color BackupStateColor(string state) => state switch
    {
        "ACTIVE" => Color.Success,
        "CREATING" or "UPDATING" => Color.Warning,
        "DELETING" or "DELETED" or "FAILED" => Color.Error,
        _ => Color.Default
    };
}
