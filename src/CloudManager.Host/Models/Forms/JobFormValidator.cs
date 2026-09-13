namespace CloudManager.Host.Models.Forms;

using FluentValidation;

using Mofucat.JobScheduler;

public sealed class JobFormValidator : AbstractValidator<JobForm>
{
    public JobFormValidator()
    {
        RuleFor(static x => x.Name)
            .NotEmpty().WithMessage("ジョブ名を入力してください。");
        RuleFor(static x => x.ProfileName)
            .NotEmpty().WithMessage("プロファイルを入力してください。");
        RuleFor(static x => x.RegionName)
            .NotEmpty().WithMessage("リージョンを入力してください。");
        RuleFor(static x => x.Operation)
            .Must(static (form, operation) => JobOperationCatalog.ForService(form.ServiceType).Contains(operation))
            .WithMessage("サービスに対応した操作を選択してください。");
        RuleFor(static x => x.CronExpression)
            .NotEmpty().WithMessage("Cron 式を入力してください。")
            .Must(BeValidCron).WithMessage("Cron 式の形式が正しくありません。");

        RuleFor(static x => x.InstanceId)
            .NotEmpty().WithMessage("Compute インスタンスの OCID を入力してください。")
            .Must(x => BeOcid(x, "instance")).WithMessage("Compute インスタンスの OCID の形式が正しくありません。")
            .When(static x => x.Operation is JobOperation.ComputeStart or JobOperation.ComputeStop or JobOperation.ComputeReboot);
        RuleFor(static x => x.DatabaseId)
            .NotEmpty().WithMessage("Autonomous Database の OCID を入力してください。")
            .Must(x => BeOcid(x, "autonomousdatabase")).WithMessage("Autonomous Database の OCID の形式が正しくありません。")
            .When(static x => x.Operation is JobOperation.AdbStart or JobOperation.AdbStop);
        RuleFor(static x => x.ContainerInstanceId)
            .NotEmpty().WithMessage("Container Instance の OCID を入力してください。")
            .Must(x => BeOcid(x, "containerinstance")).WithMessage("Container Instance の OCID の形式が正しくありません。")
            .When(static x => x.Operation is JobOperation.ContainerInstanceStart or JobOperation.ContainerInstanceStop);
        RuleFor(static x => x.FunctionId)
            .NotEmpty().WithMessage("Functions の関数 OCID を入力してください。")
            .Must(x => BeOcid(x, "fnfunc")).WithMessage("Functions の関数 OCID の形式が正しくありません。")
            .When(static x => x.Operation == JobOperation.FunctionsInvoke);
        RuleFor(static x => x.InvokeType)
            .Must(static x => x is FunctionsService.InvokeTypeSync or FunctionsService.InvokeTypeDetached)
            .WithMessage("実行タイプを選択してください。")
            .When(static x => x.Operation == JobOperation.FunctionsInvoke);
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var result = await ValidateAsync(ValidationContext<JobForm>.CreateWithOptions((JobForm)model, x => x.IncludeProperties(propertyName)));
        return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
    };

    private static bool BeValidCron(string expression)
    {
        try
        {
            _ = CronExpression.Parse(expression);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return false;
        }
    }

    // OCIDs look like ocid1.<type>.<realm>.[region].<id>
    private static bool BeOcid(string value, string resourceType) =>
        String.IsNullOrEmpty(value) || value.StartsWith($"ocid1.{resourceType}.", StringComparison.Ordinal);
}
