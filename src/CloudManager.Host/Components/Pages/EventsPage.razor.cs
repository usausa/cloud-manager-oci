namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public sealed partial class EventsPage
{
    private List<EventRuleInfo> rules = [];

    private string searchText = string.Empty;

    [Inject]
    public required EventsService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            rules = await Service.ListRulesAsync(CancellationToken);
        });

    private bool FilterFunc(EventRuleInfo rule) =>
        String.IsNullOrWhiteSpace(searchText) || rule.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase);

    private Task EnableAsync(EventRuleInfo rule) =>
        RunAsync("有効化中...", async (_, cancellationToken) =>
        {
            await Service.EnableRuleAsync(rule.Id, cancellationToken);
            Snackbar.AddSuccess($"ルールを有効化しました: {rule.DisplayName}");
            await LoadAsync();
        });

    private Task DisableAsync(EventRuleInfo rule) =>
        RunAsync("無効化中...", async (_, cancellationToken) =>
        {
            await Service.DisableRuleAsync(rule.Id, cancellationToken);
            Snackbar.AddSuccess($"ルールを無効化しました: {rule.DisplayName}");
            await LoadAsync();
        });
}
