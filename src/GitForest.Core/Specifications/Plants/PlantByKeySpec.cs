using Ardalis.Specification;

namespace GitForest.Core.Specifications.Plants;

public sealed class PlantByKeySpec : Specification<Plant>, ISingleResultSpecification<Plant>
{
    public PlantByKeySpec(string key)
    {
        Query.Where(p => p.Key == key);
    }
}


