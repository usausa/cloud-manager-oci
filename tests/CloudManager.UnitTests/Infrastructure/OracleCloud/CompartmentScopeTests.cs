namespace CloudManager.Infrastructure.OracleCloud;

using CloudManager.Models.OracleCloud.Identity;

public sealed class CompartmentScopeTests
{
    private static readonly CompartmentInfo[] Compartments =
    [
        new("root", "tenancy", "tenancy", null, 0),
        new("a", "a", "tenancy / a", "root", 1),
        new("a1", "a1", "tenancy / a / a1", "a", 2),
        new("b", "b", "tenancy / b", "root", 1)
    ];

    // The root covers every compartment of the tenancy
    [Fact]
    public void RootCoversWholeTenancy()
    {
        Assert.Equal(["root", "a", "a1", "b"], CompartmentScope.Subtree(Compartments, "root"));
    }

    // A compartment covers itself and its descendants, not its siblings
    [Fact]
    public void CompartmentCoversDescendants()
    {
        Assert.Equal(["a", "a1"], CompartmentScope.Subtree(Compartments, "a"));
        Assert.Equal(["b"], CompartmentScope.Subtree(Compartments, "b"));
    }

    // Before the tree is loaded only the selection itself is covered
    [Fact]
    public void UnknownCompartmentCoversItself()
    {
        Assert.Equal(["x"], CompartmentScope.Subtree([], "x"));
    }
}
