namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;
using CloudManager.Host.Infrastructure.Components;
using CloudManager.Host.Infrastructure.Jobs;
using CloudManager.Host.Models.Forms;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Smart.Mapper;

public sealed partial class JobsPage
{
    private static readonly DialogOptions EditDialogOptions = new() { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };

    private List<JobRow> rows = [];

    [Inject]
    public required JobService JobService { get; set; }

    [Inject]
    public required JobManager Manager { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            var jobs = await JobService.QueryAllAsync(CancellationToken);
#pragma warning disable IDE0028
            rows = jobs.Select(x => new JobRow(x, Manager.GetNextExecutionTime(x))).ToList();
#pragma warning restore IDE0028
        });

    private async Task AddAsync()
    {
        // New jobs default to the profile and region of the current session
        var form = await ShowEditDialog("ジョブ追加", new JobForm
        {
            ProfileName = Session.ProfileName,
            RegionName = Session.Region?.RegionId ?? string.Empty
        });
        if (form is null)
        {
            return;
        }

        await RunAsync("追加中...", async (_, cancellationToken) =>
        {
            await Manager.AddAsync(ToDefinition(form), cancellationToken);
            Snackbar.AddSuccess("ジョブを追加しました。");
        }, LoadAsync);
    }

    private async Task EditAsync(JobDefinition job)
    {
        var form = await ShowEditDialog("ジョブ編集", ToForm(job));
        if (form is null)
        {
            return;
        }

        await RunAsync("更新中...", async (_, cancellationToken) =>
        {
            if (await Manager.UpdateAsync(ToDefinition(form), cancellationToken))
            {
                Snackbar.AddSuccess("ジョブを更新しました。");
            }
            else
            {
                Snackbar.AddError("対象が存在しません。");
            }
        }, LoadAsync);
    }

    private async Task DeleteAsync(JobDefinition job)
    {
        if (!await DialogService.ShowConfirm("ジョブ削除", $"ジョブ「{job.Name}」を削除しますか？"))
        {
            return;
        }

        await RunAsync("削除中...", async (_, cancellationToken) =>
        {
            if (await Manager.DeleteAsync(job.Id, cancellationToken))
            {
                Snackbar.AddSuccess("ジョブを削除しました。");
            }
            else
            {
                Snackbar.AddError("対象が存在しません。");
            }
        }, LoadAsync);
    }

    private async Task RunNowAsync(JobDefinition job)
    {
        if (!await DialogService.ShowConfirm("即時実行", $"ジョブ「{job.Name}」を今すぐ実行しますか？"))
        {
            return;
        }

        await RunAsync($"実行中: {job.Name}", async (_, _) =>
        {
            var status = await Manager.ExecuteNowAsync(job);
            if (status == JobExecutionStatus.Success)
            {
                Snackbar.AddSuccess($"ジョブを実行しました: {job.Name}");
            }
            else
            {
                Snackbar.AddWarning($"ジョブが失敗しました: {job.Name} (実行履歴を確認してください)");
            }
        }, LoadAsync);
    }

    private async Task<JobForm?> ShowEditDialog(string title, JobForm form)
    {
        var reference = await DialogService.ShowAsync<JobEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(JobEditDialog.Title), title },
                { nameof(JobEditDialog.Form), form }
            },
            EditDialogOptions);
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (JobForm)result.Data! : null;
    }

    // Common fields use the generated mapper, parameters are expanded per operation
    [Mapper]
    [AfterMap(nameof(ExpandParameters))]
    private static partial JobForm ToForm(JobDefinition job);

    private static void ExpandParameters(JobDefinition job, JobForm form)
    {
        switch (job.Parameters)
        {
            case ComputeInstanceParameters p:
                form.InstanceId = p.InstanceId;
                break;
            case AutonomousDatabaseParameters p:
                form.DatabaseId = p.DatabaseId;
                break;
            case ContainerInstanceParameters p:
                form.ContainerInstanceId = p.ContainerInstanceId;
                break;
            case FunctionsInvokeParameters p:
                form.FunctionId = p.FunctionId;
                form.Payload = p.Payload;
                form.InvokeType = p.InvokeType;
                break;
        }
    }

    private static JobDefinition ToDefinition(JobForm form) => new(
        form.Id,
        form.Name,
        String.IsNullOrWhiteSpace(form.Description) ? null : form.Description,
        form.ProfileName,
        form.RegionName,
        form.ServiceType,
        form.Operation,
        ToParameters(form),
        form.CronExpression,
        form.CronTimeZone,
        form.IsEnabled,
        form.CreatedAt,
        form.CreatedAt);

    private static JobParameters ToParameters(JobForm form) => form.Operation switch
    {
        JobOperation.ComputeStart or JobOperation.ComputeStop or JobOperation.ComputeReboot => new ComputeInstanceParameters(form.InstanceId),
        JobOperation.AdbStart or JobOperation.AdbStop => new AutonomousDatabaseParameters(form.DatabaseId),
        JobOperation.ContainerInstanceStart or JobOperation.ContainerInstanceStop => new ContainerInstanceParameters(form.ContainerInstanceId),
        JobOperation.FunctionsInvoke => new FunctionsInvokeParameters(form.FunctionId, String.IsNullOrWhiteSpace(form.Payload) ? null : form.Payload, form.InvokeType),
        _ => throw new InvalidOperationException($"Unsupported operation. operation=[{form.Operation}]")
    };

    private sealed record JobRow(JobDefinition Job, DateTimeOffset? NextExecution);
}
