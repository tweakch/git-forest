using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plants;

public sealed class AllPlantsSpec : Specification<Plant>
{
    public AllPlantsSpec()
    {
        Query.OrderBy(p => p.Key);
    }
}

