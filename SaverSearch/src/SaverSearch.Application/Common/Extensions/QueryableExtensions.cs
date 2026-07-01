using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SaverSearch.Application.Common.Models;

namespace SaverSearch.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, string sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return query;

        // Normalise name (Title vs Name, etc.)
        var propertyName = sortBy;
        
        var property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (property == null) return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.MakeMemberAccess(parameter, property);
        var orderByExpression = Expression.Lambda(propertyAccess, parameter);

        var methodName = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "OrderByDescending" : "OrderBy";
        
        var resultExpression = Expression.Call(
            typeof(Queryable), 
            methodName, 
            new Type[] { typeof(T), property.PropertyType },
            query.Expression, 
            Expression.Quote(orderByExpression)
        );

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    public static async Task<PaginatedList<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> query, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, count, pageNumber, pageSize);
    }
}
