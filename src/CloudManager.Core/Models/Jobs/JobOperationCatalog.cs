namespace CloudManager.Models.Jobs;

// Selectable operations and display names per service
public static class JobOperationCatalog
{
    public static IReadOnlyList<JobOperation> ForService(JobServiceType type) => type switch
    {
        JobServiceType.Compute => [JobOperation.ComputeStart, JobOperation.ComputeStop, JobOperation.ComputeReboot],
        JobServiceType.AutonomousDatabase => [JobOperation.AdbStart, JobOperation.AdbStop],
        JobServiceType.ContainerInstance => [JobOperation.ContainerInstanceStart, JobOperation.ContainerInstanceStop],
        JobServiceType.Functions => [JobOperation.FunctionsInvoke],
        _ => []
    };

    public static string DisplayName(JobOperation operation) => operation switch
    {
        JobOperation.ComputeStart => "Compute 起動",
        JobOperation.ComputeStop => "Compute 停止",
        JobOperation.ComputeReboot => "Compute 再起動",
        JobOperation.AdbStart => "ADB 起動",
        JobOperation.AdbStop => "ADB 停止",
        JobOperation.ContainerInstanceStart => "Container Instance 起動",
        JobOperation.ContainerInstanceStop => "Container Instance 停止",
        JobOperation.FunctionsInvoke => "Functions 実行",
        _ => operation.ToString()
    };
}
