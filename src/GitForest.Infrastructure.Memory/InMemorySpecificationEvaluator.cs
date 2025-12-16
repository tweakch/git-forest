using System.Linq.Expressions;
using Ardalis.Specification;

namespace GitForest.Infrastructure.Memory;

/// <summary>
/// Evaluates Ardalis specifications against in-memory collections (IEnumerable).
/// We intentionally support only the subset of spec features we currently use: Where + Order + Skip/Take + Selector.
/// </summary>
internal static class InMemorySpecificationEvaluator
{
    public static IEnumerable<T> Apply<T>(IEnumerable<T> source, ISpecification<T> specification)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        IEnumerable<T> query = source;

        // Where
        var whereExpressionsObj = GetPropertyValue(specification, "WhereExpressions") as System.Collections.IEnumerable;
        if (whereExpressionsObj is not null)
        {
            foreach (var whereExprInfo in whereExpressionsObj)
            {
                var predicateObj = GetPropertyValue(whereExprInfo, "Filter")
                                   ?? GetPropertyValue(whereExprInfo, "Predicate")
                                   ?? GetPropertyValue(whereExprInfo, "Criteria");
                var predicate = CompilePredicate<T>(predicateObj);
                if (predicate is not null)
                {
                    query = query.Where(predicate);
                }
            }
        }

        // Ordering
        var orderExpressionsObj = GetPropertyValue(specification, "OrderExpressions") as System.Collections.IEnumerable;
        if (orderExpressionsObj is not null)
        {
            IOrderedEnumerable<T>? ordered = null;
            foreach (var orderExprInfo in orderExpressionsObj)
            {
                var keySelectorObj = GetPropertyValue(orderExprInfo, "KeySelector")
                                     ?? GetPropertyValue(orderExprInfo, "KeySelectorExpression")
                                     ?? GetPropertyValue(orderExprInfo, "Expression");
                var keySelector = CompileKeySelector<T>(keySelectorObj);
                if (keySelector is null)
                {
                    continue;
                }

                var orderTypeObj = GetPropertyValue(orderExprInfo, "OrderType")
                                   ?? GetPropertyValue(orderExprInfo, "OrderTypeEnum");
                var orderTypeText = orderTypeObj?.ToString() ?? string.Empty;
                var descending = orderTypeText.Contains("Desc", StringComparison.OrdinalIgnoreCase);

                if (ordered is null)
                {
                    ordered = descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
                }
                else
                {
                    ordered = descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector);
                }
            }

            if (ordered is not null)
            {
                query = ordered;
            }
        }

        // Skip/Take
        var skip = GetIntPropertyValue(specification, "Skip");
        if (skip.HasValue && skip.Value > 0)
        {
            query = query.Skip(skip.Value);
        }

        var take = GetIntPropertyValue(specification, "Take");
        if (take.HasValue && take.Value > 0)
        {
            query = query.Take(take.Value);
        }

        // Post processing (if present)
        var postProcessObj = GetPropertyValue(specification, "PostProcessingAction");
        if (postProcessObj is not null)
        {
            // Signature is typically Func<IEnumerable<T>, IEnumerable<T>>
            if (postProcessObj is Func<IEnumerable<T>, IEnumerable<T>> typed)
            {
                query = typed(query);
            }
            else if (postProcessObj is Delegate del)
            {
                var result = del.DynamicInvoke(query);
                if (result is IEnumerable<T> asEnumerable)
                {
                    query = asEnumerable;
                }
            }
        }

        return query;
    }

    public static IEnumerable<TResult> Apply<T, TResult>(IEnumerable<T> source, ISpecification<T, TResult> specification)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (specification is null) throw new ArgumentNullException(nameof(specification));

        var filtered = Apply(source, (ISpecification<T>)specification);

        var selectorObj = GetPropertyValue(specification, "Selector");
        if (selectorObj is null)
        {
            // No selector: best-effort cast (this matches how some specs may be constructed).
            return filtered.Cast<TResult>();
        }

        if (selectorObj is Expression<Func<T, TResult>> typedExpr)
        {
            var func = typedExpr.Compile();
            return filtered.Select(func);
        }

        if (selectorObj is LambdaExpression lambda)
        {
            var del = lambda.Compile();
            return filtered.Select(x => (TResult)del.DynamicInvoke(x)!);
        }

        return filtered.Cast<TResult>();
    }

    private static object? GetPropertyValue(object? instance, string propertyName)
    {
        if (instance is null) return null;
        var prop = instance.GetType().GetProperty(propertyName);
        return prop?.GetValue(instance);
    }

    private static int? GetIntPropertyValue(object instance, string propertyName)
    {
        var obj = GetPropertyValue(instance, propertyName);
        if (obj is null) return null;
        if (obj is int i) return i;
        if (int.TryParse(obj.ToString(), out var parsed)) return parsed;
        return null;
    }

    private static Func<T, bool>? CompilePredicate<T>(object? predicateObj)
    {
        if (predicateObj is null) return null;

        if (predicateObj is Expression<Func<T, bool>> typed)
        {
            return typed.Compile();
        }

        if (predicateObj is LambdaExpression lambda)
        {
            var del = lambda.Compile();
            return x => (bool)del.DynamicInvoke(x)!;
        }

        return null;
    }

    private static Func<T, object?>? CompileKeySelector<T>(object? keySelectorObj)
    {
        if (keySelectorObj is null) return null;

        if (keySelectorObj is LambdaExpression lambda)
        {
            var del = lambda.Compile();
            return x => del.DynamicInvoke(x);
        }

        return null;
    }
}

