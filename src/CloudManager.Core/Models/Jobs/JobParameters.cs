namespace CloudManager.Models.Jobs;

using System.Text.Json.Serialization;

// Per-operation parameters, serialized as JSON with a type discriminator
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(ComputeInstanceParameters), nameof(ComputeInstanceParameters))]
[JsonDerivedType(typeof(AutonomousDatabaseParameters), nameof(AutonomousDatabaseParameters))]
[JsonDerivedType(typeof(ContainerInstanceParameters), nameof(ContainerInstanceParameters))]
[JsonDerivedType(typeof(FunctionsInvokeParameters), nameof(FunctionsInvokeParameters))]
public abstract record JobParameters;

public sealed record ComputeInstanceParameters(string InstanceId) : JobParameters;

public sealed record AutonomousDatabaseParameters(string DatabaseId) : JobParameters;

public sealed record ContainerInstanceParameters(string ContainerInstanceId) : JobParameters;

public sealed record FunctionsInvokeParameters(string FunctionId, string? Payload, string InvokeType) : JobParameters;
