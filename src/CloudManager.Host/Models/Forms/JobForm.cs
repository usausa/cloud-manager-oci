namespace CloudManager.Host.Models.Forms;

public sealed class JobForm
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ProfileName { get; set; } = string.Empty;

    public string RegionName { get; set; } = string.Empty;

    public JobServiceType ServiceType { get; set; } = JobServiceType.Compute;

    public JobOperation Operation { get; set; } = JobOperation.ComputeStart;

    public string CronExpression { get; set; } = "0 9 * * 1-5";

    public JobCronTimeZone CronTimeZone { get; set; } = JobCronTimeZone.Local;

    public bool IsEnabled { get; set; } = true;

    // Compute
    public string InstanceId { get; set; } = string.Empty;

    // Autonomous Database
    public string DatabaseId { get; set; } = string.Empty;

    // Container Instance
    public string ContainerInstanceId { get; set; } = string.Empty;

    // Functions
    public string FunctionId { get; set; } = string.Empty;

    public string? Payload { get; set; }

    public string InvokeType { get; set; } = FunctionsService.InvokeTypeDetached;

    public DateTime CreatedAt { get; set; }
}
