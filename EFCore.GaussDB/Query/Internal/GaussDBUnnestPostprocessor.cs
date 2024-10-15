using System.Diagnostics.CodeAnalysis;
using GaussDB.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

namespace GaussDB.EntityFrameworkCore.PostgreSQL.Query.Internal;

/// <summary>
///     Locates instances of <see cref="PgUnnestExpression" /> in the tree and prunes the WITH ORDINALITY clause from them if the
///     ordinality column isn't referenced anywhere.
/// </summary>
/// <remarks>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </remarks>
public class GaussDBUnnestPostprocessor : ExpressionVisitor
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    [return: NotNullIfNotNull("expression")]
    public override Expression? Visit(Expression? expression)
    {
        switch (expression)
        {
            case ShapedQueryExpression shapedQueryExpression:
                return shapedQueryExpression.UpdateQueryExpression(Visit(shapedQueryExpression.QueryExpression));

            case SelectExpression selectExpression:
            {
                TableExpressionBase[]? newTables = null;

                for (var i = 0; i < selectExpression.Tables.Count; i++)
                {
                    var table = selectExpression.Tables[i];

                    // Find any unnest table which does not have any references to its ordinality column in the projection or orderings
                    // (this is where they may appear when a column is an identifier).
                    var unnest = table as PgUnnestExpression ?? (table as JoinExpressionBase)?.Table as PgUnnestExpression;
                    if (unnest is not null
                        && !selectExpression.Orderings.Select(o => o.Expression)
                            .Concat(selectExpression.Projection.Select(p => p.Expression))
                            .Any(
                                p => p is ColumnExpression { Name: "ordinality", Table: var ordinalityTable }
                                    && ordinalityTable == table))
                    {
                        if (newTables is null)
                        {
                            newTables = new TableExpressionBase[selectExpression.Tables.Count];

                            for (var j = 0; j < i; j++)
                            {
                                newTables[j] = selectExpression.Tables[j];
                            }
                        }

                        var newUnnest = new PgUnnestExpression(unnest.Alias, unnest.Array, unnest.ColumnName, withOrdinality: false);

                        newTables[i] = table switch
                        {
                            JoinExpressionBase j => j.Update(newUnnest),
                            PgUnnestExpression => newUnnest,
                            _ => throw new UnreachableException()
                        };
                    }
                }

                return base.Visit(
                    newTables is null
                        ? selectExpression
                        : selectExpression.Update(
                            selectExpression.Projection,
                            newTables,
                            selectExpression.Predicate,
                            selectExpression.GroupBy,
                            selectExpression.Having,
                            selectExpression.Orderings,
                            selectExpression.Limit,
                            selectExpression.Offset));
            }

            default:
                return base.Visit(expression);
        }
    }
}
