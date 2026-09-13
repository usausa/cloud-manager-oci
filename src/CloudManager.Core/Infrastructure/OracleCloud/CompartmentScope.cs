namespace CloudManager.Infrastructure.OracleCloud;

using CloudManager.Models.OracleCloud.Identity;

// Compartments covered by a selection: the compartment itself followed by its descendants
public static class CompartmentScope
{
    public static IReadOnlyList<string> Subtree(IReadOnlyList<CompartmentInfo> compartments, string compartmentId)
    {
        var parents = compartments.ToDictionary(static x => x.Id, static x => x.ParentId, StringComparer.Ordinal);
        var result = new List<string> { compartmentId };
        foreach (var compartment in compartments)
        {
            if (IsDescendant(parents, compartment.Id, compartmentId))
            {
                result.Add(compartment.Id);
            }
        }

        return result;
    }

    private static bool IsDescendant(Dictionary<string, string?> parents, string id, string ancestorId)
    {
        var current = parents.GetValueOrDefault(id);
        while (!String.IsNullOrEmpty(current))
        {
            if (String.Equals(current, ancestorId, StringComparison.Ordinal))
            {
                return true;
            }

            current = parents.GetValueOrDefault(current);
        }

        return false;
    }
}
