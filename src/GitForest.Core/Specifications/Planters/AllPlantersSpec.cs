using Ardalis.Specification;

namespace GitForest.Core.Specifications.Planters;

public sealed class AllPlantersSpec : Specification<Planter>
{
    public AllPlantersSpec()
    {
        Query.OrderBy(p => p.Id);
    }
}
