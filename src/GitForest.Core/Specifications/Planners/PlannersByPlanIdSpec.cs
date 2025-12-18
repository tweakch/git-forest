using Ardalis.Specification;

namespace GitForest.Core.Specifications.Planners;

public sealed class PlannersByPlanIdSpec : Specification<Planner>
{
    public PlannersByPlanIdSpec(string planId)
    {
        Query.Where(p => p.PlanId == planId).OrderBy(p => p.Id);
    }
}
