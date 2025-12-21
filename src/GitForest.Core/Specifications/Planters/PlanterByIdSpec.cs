using Ardalis.Specification;

namespace GitForest.Core.Specifications.Planters;

public sealed class PlanterByIdSpec : Specification<Planter>, ISingleResultSpecification<Planter>
{
    public PlanterByIdSpec(string planterId)
    {
        Query.Where(p => p.Id == planterId);
    }
}
