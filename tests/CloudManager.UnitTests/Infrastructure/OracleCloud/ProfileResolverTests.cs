namespace CloudManager.Infrastructure.OracleCloud;

public sealed class ProfileResolverTests
{
    // An unknown profile fails before any SDK call with a message naming the profile
    [Fact]
    public void UnknownProfileThrowsInvalidOperation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ProfileResolver.Resolve("cloudmanager-test", null, null));

        Assert.Contains("cloudmanager-test", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownProfileHasNoValues()
    {
        Assert.Null(ProfileResolver.GetProfileValue("cloudmanager-test", "tenancy"));
    }
}
