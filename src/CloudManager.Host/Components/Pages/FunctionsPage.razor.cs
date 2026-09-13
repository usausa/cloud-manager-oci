namespace CloudManager.Host.Components.Pages;

using CloudManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

public sealed partial class FunctionsPage
{
    private List<FunctionsApplicationInfo> applications = [];

    private List<FunctionInfo> functions = [];

    private string? selectedApplicationId;

    private string searchText = string.Empty;

    [Inject]
    public required FunctionsService Service { get; set; }

    protected override Task OnInitializedAsync() => LoadAsync();

    protected override Task OnSessionChangedAsync() => LoadAsync();

    // Loads the applications and keeps the selected one when it still exists
    private Task LoadAsync() =>
        LoadAsync(async () =>
        {
            applications = await Service.ListApplicationsAsync(CancellationToken);
            if ((selectedApplicationId is null) || applications.All(x => x.Id != selectedApplicationId))
            {
                selectedApplicationId = applications.FirstOrDefault()?.Id;
            }

            functions = selectedApplicationId is null ? [] : await Service.ListFunctionsAsync(selectedApplicationId, CancellationToken);
        });

    private Task OnApplicationSelectedAsync(string? applicationId)
    {
        selectedApplicationId = applicationId;
        return LoadAsync(async () =>
        {
            functions = applicationId is null ? [] : await Service.ListFunctionsAsync(applicationId, CancellationToken);
        });
    }

    private async Task InvokeAsync(FunctionInfo function)
    {
        var dialog = await DialogService.ShowAsync<FunctionsInvokeDialog>("Functions 実行", new DialogParameters<FunctionsInvokeDialog>
        {
            { x => x.FunctionName, function.DisplayName }
        },
        Styles.MediumDialog);
        var dialogResult = await dialog.Result;
        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        var invokeParams = (FunctionsInvokeParams)dialogResult.Data!;
        await RunAsync($"実行中: {function.DisplayName}", async (_, cancellationToken) =>
        {
            var result = await Service.InvokeAsync(function.Id, invokeParams.Payload, invokeParams.InvokeType, cancellationToken);

            await DialogService.ShowAsync<FunctionsResultDialog>("実行結果", new DialogParameters<FunctionsResultDialog>
            {
                { x => x.FunctionName, function.DisplayName },
                { x => x.InvokeType, invokeParams.InvokeType },
                { x => x.RequestId, result.RequestId },
                { x => x.Payload, result.Payload }
            },
            Styles.LargeDialog);
        });
    }

    private async Task ShowConfigAsync(FunctionInfo function)
    {
        await DialogService.ShowAsync<FunctionsConfigDialog>("構成", new DialogParameters<FunctionsConfigDialog>
        {
            { x => x.FunctionId, function.Id },
            { x => x.FunctionName, function.DisplayName }
        },
        Styles.MediumDialog);
    }

    private async Task ShowConcurrencyAsync(FunctionInfo function)
    {
        var dialog = await DialogService.ShowAsync<FunctionsConcurrencyDialog>("プロビジョニング済み同時実行", new DialogParameters<FunctionsConcurrencyDialog>
        {
            { x => x.FunctionId, function.Id },
            { x => x.FunctionName, function.DisplayName },
            { x => x.Current, function.ProvisionedConcurrency }
        });
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private bool FilterFunc(FunctionInfo f) =>
        String.IsNullOrWhiteSpace(searchText) || f.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
