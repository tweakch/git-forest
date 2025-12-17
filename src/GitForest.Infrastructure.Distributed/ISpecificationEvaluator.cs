using Ardalis.Specification;

namespace GitForest.Infrastructure.Distributed;

/// <summary>
/// Evaluates Ardalis specifications against collections
/// </summary>
public interface ISpecificationEvaluator
{
    IEnumerable<T> Evaluate<T>(IEnumerable<T> source, ISpecification<T> specification);
    IEnumerable<TResult> Evaluate<T, TResult>(IEnumerable<T> source, ISpecification<T, TResult> specification);
}
