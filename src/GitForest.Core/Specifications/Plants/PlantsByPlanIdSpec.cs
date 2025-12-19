using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plants;

public sealed class PlantsByPlanIdSpec : Specification<Plant>
{
    public PlantsByPlanIdSpec(string planId)
    {
        Query.Where(p => p.PlanId == planId).OrderBy(p => p.Key);
    }
}

