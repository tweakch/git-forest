using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plants;

public sealed class PlantsByPlanIdAndStatusSpec : Specification<Plant>
{
    public PlantsByPlanIdAndStatusSpec(string planId, string status)
    {
        Query.Where(p => p.PlanId == planId && p.Status == status)
             .OrderBy(p => p.Key);
    }
}


