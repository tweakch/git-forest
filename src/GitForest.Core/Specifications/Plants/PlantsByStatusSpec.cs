using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plants;

public sealed class PlantsByStatusSpec : Specification<Plant>
{
    public PlantsByStatusSpec(string status)
    {
        Query.Where(p => p.Status == status)
             .OrderBy(p => p.Key);
    }
}


