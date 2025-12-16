using Ardalis.Specification;

namespace GitForest.Core.Specifications.Planners;

public sealed class PlannerByIdSpec : Specification<Planner>, ISingleResultSpecification<Planner>
{
    public PlannerByIdSpec(string plannerId)
    {
        Query.Where(p => p.Id == plannerId);
    }
}

