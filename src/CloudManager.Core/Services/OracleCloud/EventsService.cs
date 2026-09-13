namespace CloudManager.Services.OracleCloud;

using CloudManager.Infrastructure.OracleCloud;
using CloudManager.Models.OracleCloud.Events;

using Oci.EventsService.Models;
using Oci.EventsService.Requests;

// The type name matches an SDK namespace segment
#pragma warning disable CA1724
public sealed class EventsService
{
    private const int MaxParallelLookups = 8;

    private readonly OciClientFactory factory;

    public EventsService(OciClientFactory factory)
    {
        this.factory = factory;
    }

    // Lists the rules of the compartment with their actions
    public async ValueTask<List<EventRuleInfo>> ListRulesAsync(CancellationToken cancellationToken = default)
    {
        using var events = factory.CreateEventsClient();
        var rules = await OciPaging.ListAllAsync(
            page => events.ListRules(new ListRulesRequest { CompartmentId = factory.CompartmentId, Page = page }, cancellationToken: cancellationToken),
            static x => x.Items,
            static x => x.OpcNextPage);

        // Actions are only available on the full rule
        using var semaphore = new SemaphoreSlim(MaxParallelLookups);
        var details = await Task.WhenAll(rules.Select(async rule =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var response = await events.GetRule(new GetRuleRequest { RuleId = rule.Id }, cancellationToken: cancellationToken);
                return response.Rule;
            }
            finally
            {
                semaphore.Release();
            }
        }));

#pragma warning disable IDE0028
        return details
            .Select(static x => new EventRuleInfo(
                x.Id,
                x.DisplayName,
                x.Description,
                x.IsEnabled ?? false,
                OciValues.State(x.LifecycleState),
                x.Condition,
                (x.Actions?.Actions ?? []).Select(Describe).ToList(),
                x.TimeCreated))
            .OrderBy(static x => x.DisplayName, StringComparer.Ordinal)
            .ToList();
#pragma warning restore IDE0028
    }

    public async ValueTask EnableRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        using var events = factory.CreateEventsClient();
        await events.UpdateRule(new UpdateRuleRequest { RuleId = ruleId, UpdateRuleDetails = new UpdateRuleDetails { IsEnabled = true } }, cancellationToken: cancellationToken);
    }

    public async ValueTask DisableRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        using var events = factory.CreateEventsClient();
        await events.UpdateRule(new UpdateRuleRequest { RuleId = ruleId, UpdateRuleDetails = new UpdateRuleDetails { IsEnabled = false } }, cancellationToken: cancellationToken);
    }

    // Shows the action kind with its target
    private static string Describe(Action action)
    {
        var target = action switch
        {
            NotificationServiceAction x => x.TopicId,
            StreamingServiceAction x => x.StreamId,
            FaaSAction x => x.FunctionId,
            _ => action.Id
        };
        var kind = action.GetType().Name.Replace("Action", string.Empty, StringComparison.Ordinal);
        return String.IsNullOrEmpty(action.Description) ? $"{kind}: {target}" : $"{kind}: {action.Description}";
    }
}
#pragma warning restore CA1724
