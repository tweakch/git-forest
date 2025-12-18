using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plans;

public sealed class PlanByIdSpec : Specification<Plan>, ISingleResultSpecification<Plan>
{
    public PlanByIdSpec(string planId)
    {
        Query.Where(p => p.Id == planId);
    }
}

