namespace CloudManager.Models.Forms;

using CloudManager.Host.Models.Forms;
using CloudManager.Models.Jobs;

public sealed class JobFormValidatorTests
{
    private static readonly JobFormValidator Validator = new();

    private static JobForm CreateValidForm() => new()
    {
        Name = "job",
        ProfileName = "DEFAULT",
        RegionName = "ap-tokyo-1",
        ServiceType = JobServiceType.Compute,
        Operation = JobOperation.ComputeStart,
        InstanceId = "ocid1.instance.oc1.ap-tokyo-1.example",
        CronExpression = "0 9 * * 1-5"
    };

    [Fact]
    public void ValidFormPasses()
    {
        var result = Validator.Validate(CreateValidForm());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("0 25 * * *")]
    public void InvalidCronFails(string cron)
    {
        var form = CreateValidForm();
        form.CronExpression = cron;

        var result = Validator.Validate(form);

        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.CronExpression));
    }

    // Only the fields required by the operation are validated
    [Fact]
    public void OperationSpecificParametersAreRequired()
    {
        var form = CreateValidForm();
        form.ServiceType = JobServiceType.Functions;
        form.Operation = JobOperation.FunctionsInvoke;
        form.InstanceId = string.Empty;

        var result = Validator.Validate(form);

        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.FunctionId));
        Assert.DoesNotContain(result.Errors, static x => x.PropertyName == nameof(JobForm.InstanceId));
    }

    [Fact]
    public void OperationMustBelongToService()
    {
        var form = CreateValidForm();
        form.ServiceType = JobServiceType.AutonomousDatabase;

        var result = Validator.Validate(form);

        Assert.Contains(result.Errors, static x => x.PropertyName == nameof(JobForm.Operation));
    }

    // Resource identifiers must be OCIDs of the expected type
    [Theory]
    [InlineData("ocid1.instance.oc1.ap-tokyo-1.example", true)]
    [InlineData("ocid1.autonomousdatabase.oc1.ap-tokyo-1.example", false)]
    [InlineData("i-0123456789abcdef0", false)]
    public void InstanceIdMustBeInstanceOcid(string instanceId, bool valid)
    {
        var form = CreateValidForm();
        form.InstanceId = instanceId;

        var result = Validator.Validate(form);

        Assert.Equal(valid, !result.Errors.Any(static x => x.PropertyName == nameof(JobForm.InstanceId)));
    }
}
