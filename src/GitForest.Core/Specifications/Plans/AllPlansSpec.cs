using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plans;

public sealed class AllPlansSpec : Specification<Plan>
{
    public AllPlansSpec()
    {
        Query.OrderBy(p => p.Id);
    }
}
