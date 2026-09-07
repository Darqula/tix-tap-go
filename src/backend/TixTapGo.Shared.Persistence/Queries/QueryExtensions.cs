using System.Linq.Expressions;

namespace TixTapGo.Shared.Persistence.Queries;

public static class QueryExtensions
{
    public static IQueryable<TItem> WhereIf<TItem>(this IQueryable<TItem> source, bool condition,
        Expression<Func<TItem, bool>> predicate) => condition ? source.Where(predicate) : source;
}
