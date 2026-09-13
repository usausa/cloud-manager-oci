namespace CloudManager.Models.Jobs;

public enum JobOperation
{
    // Compute
    ComputeStart,
    ComputeStop,
    ComputeReboot,
    // Autonomous Database
    AdbStart,
    AdbStop,
    // Container Instance
    ContainerInstanceStart,
    ContainerInstanceStop,
    // Functions
    FunctionsInvoke
}
