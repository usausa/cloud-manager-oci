namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Functions;

using Oci.FunctionsService.Models;
using Oci.FunctionsService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class FunctionsService
{
    public const string InvokeTypeSync = "Sync";
    public const string InvokeTypeDetached = "Detached";

    private readonly OciClientFactory factory;

    public FunctionsService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the applications of the compartments in scope
    public async ValueTask<List<FunctionsApplicationInfo>> ListApplicationsAsync(CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var applications = await factory.ListInScopeAsync(
            management,
            (client, compartmentId, page) => client.ListApplications(new ListApplicationsRequest { CompartmentId = compartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage,
            cancellationToken);

#pragma warning disable IDE0028
        return applications
            .Select(static x => new FunctionsApplicationInfo(
                x.Id,
                x.CompartmentId,
                x.DisplayName,
                OciValues.State(x.LifecycleState),
                OciValues.State(x.Shape),
                x.TimeCreated.GetValueOrDefault()))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Lists the functions of an application
    public async ValueTask<List<FunctionInfo>> ListFunctionsAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var functions = await OciPaging.ListAllAsync(
            page => management.ListFunctions(new ListFunctionsRequest { ApplicationId = applicationId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

#pragma warning disable IDE0028
        return functions
            .Select(static x => new FunctionInfo(
                x.Id,
                x.DisplayName,
                x.ApplicationId,
                OciValues.State(x.LifecycleState),
                x.Image,
                x.MemoryInMBs.HasValue ? (int)x.MemoryInMBs.Value : null,
                x.TimeoutInSeconds,
                x.InvokeEndpoint,
                (x.ProvisionedConcurrencyConfig as ConstantProvisionedConcurrencyConfig)?.Count,
                x.TimeUpdated))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    // Invokes a function through its invoke endpoint and returns the response body
    public async ValueTask<FunctionsInvokeResult> InvokeAsync(string functionId, string? payload, string invokeType, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var function = await management.GetFunction(new GetFunctionRequest { FunctionId = functionId }, cancellationToken: cancellationToken);

        using var invoke = factory.CreateFunctionsInvokeClient(function.Function.InvokeEndpoint);
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload ?? string.Empty));
        var response = await invoke.InvokeFunction(
            new InvokeFunctionRequest
            {
                FunctionId = functionId,
                InvokeFunctionBody = body,
                FnInvokeType = String.Equals(invokeType, InvokeTypeDetached, StringComparison.OrdinalIgnoreCase)
                    ? InvokeFunctionRequest.FnInvokeTypeEnum.Detached
                    : InvokeFunctionRequest.FnInvokeTypeEnum.Sync
            },
            cancellationToken: cancellationToken);

        var result = string.Empty;
        if (response.InputStream is not null)
        {
            using var reader = new StreamReader(response.InputStream, Encoding.UTF8);
            result = await reader.ReadToEndAsync(cancellationToken);
        }

        return new FunctionsInvokeResult(result, response.OpcRequestId);
    }

    // Configuration entries of a function
    public async ValueTask<List<FunctionsConfigEntry>> GetConfigAsync(string functionId, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var function = await management.GetFunction(new GetFunctionRequest { FunctionId = functionId }, cancellationToken: cancellationToken);
        var config = function.Function.Config;
#pragma warning disable IDE0028
        return config is null
            ? []
            : config.Select(static x => new FunctionsConfigEntry(x.Key, x.Value)).OrderBy(static x => x.Key, StringComparer.Ordinal).ToList();
#pragma warning restore IDE0028
    }

    // Replaces the configuration entries of a function
    public async ValueTask UpdateConfigAsync(string functionId, Dictionary<string, string> config, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        await management.UpdateFunction(
            new UpdateFunctionRequest
            {
                FunctionId = functionId,
                UpdateFunctionDetails = new UpdateFunctionDetails { Config = config }
            },
            cancellationToken: cancellationToken);
    }

    // Sets provisioned concurrency, null disables it
    public async ValueTask SetProvisionedConcurrencyAsync(string functionId, int? count, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        await management.UpdateFunction(
            new UpdateFunctionRequest
            {
                FunctionId = functionId,
                UpdateFunctionDetails = new UpdateFunctionDetails
                {
                    ProvisionedConcurrencyConfig = count.HasValue
                        ? new ConstantProvisionedConcurrencyConfig { Count = count.Value }
                        : new NoneProvisionedConcurrencyConfig()
                }
            },
            cancellationToken: cancellationToken);
    }
}
#pragma warning restore CA1724
