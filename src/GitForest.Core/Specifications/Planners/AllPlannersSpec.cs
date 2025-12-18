using Ardalis.Specification;

namespace GitForest.Core.Specifications.Planners;

public sealed class AllPlannersSpec : Specification<Planner>
{
    public AllPlannersSpec()
    {
        Query.OrderBy(p => p.Id);
    }
}
