namespace CloudManager;

using BunnyTail.ServiceRegistration;

using Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    [ServiceRegistration(Lifetime.Singleton, "^Job.*Service$")]
    public static partial IServiceCollection AddCoreServices(this IServiceCollection services);

    // OCI services are scoped because they depend on the per-circuit OciClientFactory
    [ServiceRegistration(Lifetime.Scoped, "Service$", Namespace = "CloudManager.Services.OracleCloud")]
    public static partial IServiceCollection AddOciServices(this IServiceCollection services);
}
