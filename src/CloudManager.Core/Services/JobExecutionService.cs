namespace CloudManager.Services;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.Jobs;
using CloudManager.Services.OracleCloud;

using Oci.Common.Model;

// Executes the OCI operation of a job definition and records the result
public sealed class JobExecutionService
{
    private readonly ILogger<JobExecutionService> log;

    private readonly JobExecutionOptions options;

    private readonly JobLogService jobLogService;

    public JobExecutionService(
        ILogger<JobExecutionService> log,
        JobExecutionOptions options,
        JobLogService jobLogService)
    {
        this.log = log;
        this.options = options;
        this.jobLogService = jobLogService;
    }

    // Returns the resulting JobExecutionStatus
    public async ValueTask<string> ExecuteAsync(JobDefinition job, CancellationToken cancellationToken)
    {
        log.InfoJobStart(job.Id, job.Name, job.Operation);
        var logId = await jobLogService.StartAsync(job.Id, job.Name, cancellationToken);

        string status;
        string? message = null;
        string? errorDetail = null;
        try
        {
            // Jobs run with the profile of the definition, independent of the UI session.
            // Operations target resources by OCID, so no compartment is needed.
            var factory = OciClientFactory.Create(job.ProfileName, job.RegionName, null);
            message = await DispatchAsync(factory, job, cancellationToken);
            status = JobExecutionStatus.Success;
            log.InfoJobSuccess(job.Id, job.Name, message);
        }
        catch (OperationCanceledException)
        {
            status = JobExecutionStatus.Failure;
            errorDetail = "Cancelled";
        }
        catch (OciException ex)
        {
            status = JobExecutionStatus.Failure;
            errorDetail = ex.FormatError();
            log.WarnJobFailed(job.Id, job.Name, errorDetail, ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            status = JobExecutionStatus.Failure;
            errorDetail = ex.ToString();
            log.WarnJobFailed(job.Id, job.Name, ex.Message, ex);
        }

        // Record the result even while shutting down
        await jobLogService.FinishAsync(logId, status, message, errorDetail, CancellationToken.None);
        await jobLogService.TrimAsync(job.Id, options.LogRetentionCountPerJob, CancellationToken.None);

        return status;
    }

    private static async ValueTask<string?> DispatchAsync(OciClientFactory factory, JobDefinition job, CancellationToken cancellationToken)
    {
        switch (job.Operation)
        {
            case JobOperation.ComputeStart:
            {
                var p = (ComputeInstanceParameters)job.Parameters;
                await new ComputeService(factory).StartAsync(p.InstanceId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Started: {p.InstanceId}";
            }

            case JobOperation.ComputeStop:
            {
                var p = (ComputeInstanceParameters)job.Parameters;
                await new ComputeService(factory).StopAsync(p.InstanceId, force: false, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Stopped: {p.InstanceId}";
            }

            case JobOperation.ComputeReboot:
            {
                var p = (ComputeInstanceParameters)job.Parameters;
                await new ComputeService(factory).RebootAsync(p.InstanceId, force: false, cancellationToken);
                return $"Rebooted: {p.InstanceId}";
            }

            case JobOperation.AdbStart:
            {
                var p = (AutonomousDatabaseParameters)job.Parameters;
                await new AutonomousDatabaseService(factory).StartAsync(p.DatabaseId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Started: {p.DatabaseId}";
            }

            case JobOperation.AdbStop:
            {
                var p = (AutonomousDatabaseParameters)job.Parameters;
                await new AutonomousDatabaseService(factory).StopAsync(p.DatabaseId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Stopped: {p.DatabaseId}";
            }

            case JobOperation.ContainerInstanceStart:
            {
                var p = (ContainerInstanceParameters)job.Parameters;
                await new ContainerInstanceService(factory).StartAsync(p.ContainerInstanceId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Started: {p.ContainerInstanceId}";
            }

            case JobOperation.ContainerInstanceStop:
            {
                var p = (ContainerInstanceParameters)job.Parameters;
                await new ContainerInstanceService(factory).StopAsync(p.ContainerInstanceId, wait: false, timeoutSeconds: 0, NullProgress.Instance, cancellationToken);
                return $"Stopped: {p.ContainerInstanceId}";
            }

            case JobOperation.FunctionsInvoke:
            {
                var p = (FunctionsInvokeParameters)job.Parameters;
                var result = await new FunctionsService(factory).InvokeAsync(p.FunctionId, p.Payload, p.InvokeType, cancellationToken);
                return $"Invoked: {p.FunctionId} type={p.InvokeType} length={result.Payload.Length}";
            }

            default:
                throw new InvalidOperationException($"Unsupported operation. operation=[{job.Operation}]");
        }
    }

    // Progress is not used for job execution
    private sealed class NullProgress : IProgress<ProgressUpdate>
    {
        public static NullProgress Instance { get; } = new();

        public void Report(ProgressUpdate value)
        {
        }
    }
}
