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

    private const int WorkRequestTimeoutSeconds = 600;

    private static readonly TimeSpan WorkRequestPollInterval = TimeSpan.FromSeconds(2);

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
    }

    // Lists the functions of an application
    public async ValueTask<List<FunctionInfo>> ListFunctionsAsync(string applicationId, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var functions = await OciPaging.ListAllAsync(
            page => management.ListFunctions(new ListFunctionsRequest { ApplicationId = applicationId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

        return functions
            .Select(static x => new FunctionInfo(
                x.Id,
                x.DisplayName,
                x.ApplicationId,
                OciValues.State(x.LifecycleState),
                (x.SourceDetails as ContainerImageFunctionSourceDetails)?.Image,
                x.MemoryInMBs.HasValue ? (int)x.MemoryInMBs.Value : null,
                x.TimeoutInSeconds,
                x.InvokeEndpoint,
                (x.ProvisionedConcurrencyConfig as ConstantProvisionedConcurrencyConfig)?.Count,
                x.TimeUpdated))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
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
        return config is null
            ? []
            : config.Select(static x => new FunctionsConfigEntry(x.Key, x.Value)).OrderBy(static x => x.Key, StringComparer.Ordinal).ToList();
    }

    // Replaces the configuration entries of a function
    public async ValueTask UpdateConfigAsync(string functionId, Dictionary<string, string> config, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var response = await management.UpdateFunction(
            new UpdateFunctionRequest
            {
                FunctionId = functionId,
                UpdateFunctionDetails = new UpdateFunctionDetails { Config = config }
            },
            cancellationToken: cancellationToken);

        await WaitForWorkRequestAsync(response.OpcWorkRequestId, cancellationToken);
    }

    // Sets provisioned concurrency, null disables it
    public async ValueTask SetProvisionedConcurrencyAsync(string functionId, int? count, CancellationToken cancellationToken = default)
    {
        using var management = factory.CreateFunctionsManagementClient();
        var response = await management.UpdateFunction(
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

        await WaitForWorkRequestAsync(response.OpcWorkRequestId, cancellationToken);
    }

    private async ValueTask WaitForWorkRequestAsync(string workRequestId, CancellationToken cancellationToken)
    {
        using var workRequest = factory.CreateFunctionsWorkRequestClient();
        var deadline = DateTime.UtcNow.AddSeconds(WorkRequestTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(WorkRequestPollInterval, cancellationToken);

            var response = await workRequest.GetWorkRequest(new GetWorkRequestRequest { WorkRequestId = workRequestId }, cancellationToken: cancellationToken);
            switch (response.WorkRequest.Status)
            {
                case OperationStatus.Succeeded:
                    return;
                case OperationStatus.Failed:
                case OperationStatus.Canceled:
                    throw new InvalidOperationException($"Update work request {OciValues.State(response.WorkRequest.Status)}. id=[{workRequestId}]");
            }
        }

        throw new TimeoutException($"Update work request did not complete within {WorkRequestTimeoutSeconds} seconds.");
    }
}
#pragma warning restore CA1724
