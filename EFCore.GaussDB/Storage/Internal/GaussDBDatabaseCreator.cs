using System.Net.Sockets;
using System.Transactions;
using GaussDB.EntityFrameworkCore.PostgreSQL.Migrations.Operations;

namespace GaussDB.EntityFrameworkCore.PostgreSQL.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class GaussDBDatabaseCreator : RelationalDatabaseCreator
{
    private readonly IGaussDBRelationalConnection _connection;
    private readonly IRawSqlCommandBuilder _rawSqlCommandBuilder;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public GaussDBDatabaseCreator(
        RelationalDatabaseCreatorDependencies dependencies,
        IGaussDBRelationalConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder)
        : base(dependencies)
    {
        _connection = connection;
        _rawSqlCommandBuilder = rawSqlCommandBuilder;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual TimeSpan RetryTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Create()
    {
        using (var masterConnection = _connection.CreateAdminConnection())
        {
            try
            {
                Dependencies.MigrationCommandExecutor
                    .ExecuteNonQuery(CreateCreateOperations(), masterConnection);
            }
            catch (PostgresException e) when (e is { SqlState: "23505", ConstraintName: "pg_database_datname_index" })
            {
                // This occurs when two connections are trying to create the same database concurrently
                // (happens in the tests). Simply ignore the error.
            }

            ClearPool();
        }

        Exists();
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        var masterConnection = _connection.CreateAdminConnection();
        await using (masterConnection.ConfigureAwait(false))
        {
            try
            {
                await Dependencies.MigrationCommandExecutor
                    .ExecuteNonQueryAsync(CreateCreateOperations(), masterConnection, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (PostgresException e) when (e is { SqlState: "23505", ConstraintName: "pg_database_datname_index" })
            {
                // This occurs when two connections are trying to create the same database concurrently
                // (happens in the tests). Simply ignore the error.
            }

            ClearPool();
        }

        await ExistsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool HasTables()
        => Dependencies.ExecutionStrategy
            .Execute(
                _connection,
                connection => (bool)CreateHasTablesCommand()
                    .ExecuteScalar(
                        new RelationalCommandParameterObject(
                            connection,
                            null,
                            null,
                            Dependencies.CurrentContext.Context,
                            Dependencies.CommandLogger))!);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task<bool> HasTablesAsync(CancellationToken cancellationToken = default)
        => Dependencies.ExecutionStrategy.ExecuteAsync(
            _connection,
            async (connection, ct) => (bool)(await CreateHasTablesCommand()
                .ExecuteScalarAsync(
                    new RelationalCommandParameterObject(
                        connection,
                        null,
                        null,
                        Dependencies.CurrentContext.Context,
                        Dependencies.CommandLogger),
                    cancellationToken: ct).ConfigureAwait(false))!, cancellationToken);

    private IRelationalCommand CreateHasTablesCommand()
        => _rawSqlCommandBuilder
            .Build(
                """
SELECT CASE WHEN COUNT(*) = 0 THEN FALSE ELSE TRUE END
FROM pg_class AS cls
JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
WHERE
        cls.relkind IN ('r', 'v', 'm', 'f', 'p') AND
        ns.nspname NOT IN ('pg_catalog', 'information_schema') AND
        -- Exclude tables which are members of PG extensions
        NOT EXISTS (
            SELECT 1 FROM pg_depend WHERE
                classid=(
                    SELECT cls.oid
                    FROM pg_class AS cls
                             JOIN pg_namespace AS ns ON ns.oid = cls.relnamespace
                    WHERE relname='pg_class' AND ns.nspname='pg_catalog'
                ) AND
                objid=cls.oid AND
                deptype IN ('e', 'x')
        )
""");

    private IReadOnlyList<MigrationCommand> CreateCreateOperations()
    {
        var designTimeModel = Dependencies.CurrentContext.Context.GetService<IDesignTimeModel>().Model;

        return Dependencies.MigrationsSqlGenerator.Generate(
            new[]
            {
                new GaussDBCreateDatabaseOperation
                {
                    Name = _connection.DbConnection.Database,
                    Template = designTimeModel.GetDatabaseTemplate(),
                    Collation = designTimeModel.GetCollation(),
                    Tablespace = designTimeModel.GetTablespace()
                }
            });
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override bool Exists()
        => Exists(async: false).GetAwaiter().GetResult();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
        => Exists(async: true, cancellationToken);

    private async Task<bool> Exists(bool async, CancellationToken cancellationToken = default)
    {
        // When checking whether a database exists, pooling must be off, otherwise we may
        // attempt to reuse a pooled connection, which may be broken (this happened in the tests).
        // If Pooling is off, but Multiplexing is on - GaussDBConnectionStringBuilder.Validate will throw,
        // so we turn off Multiplexing as well.
        var unpooledCsb = new GaussDBConnectionStringBuilder(_connection.ConnectionString) { Pooling = false, Multiplexing = false };

        using var _ = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
        var unpooledRelationalConnection = _connection.CloneWith(unpooledCsb.ToString());
        try
        {
            if (async)
            {
                await unpooledRelationalConnection.OpenAsync(errorsExpected: true, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                unpooledRelationalConnection.Open(errorsExpected: true);
            }

            return true;
        }
        catch (PostgresException e)
        {
            if (IsDoesNotExist(e))
            {
                return false;
            }

            throw;
        }
        catch (GaussDBException e) when (
            // This can happen when GaussDB attempts to connect to multiple hosts
            e.InnerException is AggregateException ae && ae.InnerExceptions.Any(ie => ie is PostgresException pe && IsDoesNotExist(pe)))
        {
            return false;
        }
        catch (GaussDBException e) when (
            e.InnerException is IOException { InnerException: SocketException { SocketErrorCode: SocketError.ConnectionReset } })
        {
            // Pretty awful hack around #104
            return false;
        }
        finally
        {
            if (async)
            {
                await unpooledRelationalConnection.CloseAsync().ConfigureAwait(false);
                await unpooledRelationalConnection.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                unpooledRelationalConnection.Close();
                unpooledRelationalConnection.Dispose();
            }
        }
    }

    // Login failed is thrown when database does not exist (See Issue #776)
    private static bool IsDoesNotExist(PostgresException exception)
        => exception.SqlState == "3D000";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void Delete()
    {
        ClearAllPools();

        using (var masterConnection = _connection.CreateAdminConnection())
        {
            Dependencies.MigrationCommandExecutor
                .ExecuteNonQuery(CreateDropCommands(), masterConnection);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        ClearAllPools();

        var masterConnection = _connection.CreateAdminConnection();
        await using (masterConnection)
        {
            await Dependencies.MigrationCommandExecutor
                .ExecuteNonQueryAsync(CreateDropCommands(), masterConnection, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override void CreateTables()
    {
        var designTimeModel = Dependencies.CurrentContext.Context.GetService<IDesignTimeModel>().Model;
        var operations = Dependencies.ModelDiffer.GetDifferences(null, designTimeModel.GetRelationalModel());
        var commands = Dependencies.MigrationsSqlGenerator.Generate(operations, designTimeModel);

        // If a PostgreSQL extension, enum or range was added, we want GaussDB to reload all types at the ADO.NET level.
        var reloadTypes =
            operations.OfType<AlterDatabaseOperation>()
                .Any(
                    o =>
                        o.GetPostgresExtensions().Any() || o.GetPostgresEnums().Any() || o.GetPostgresRanges().Any());

        try
        {
            Dependencies.MigrationCommandExecutor.ExecuteNonQuery(commands, _connection);
        }
        catch (PostgresException e) when (e is { SqlState: "23505", ConstraintName: "pg_type_typname_nsp_index" })
        {
            // This occurs when two connections are trying to create the same database concurrently
            // (happens in the tests). Simply ignore the error.
        }

        if (reloadTypes && _connection.DbConnection is GaussDBConnection GaussDBConnection)
        {
            GaussDBConnection.Open();
            try
            {
                GaussDBConnection.ReloadTypes();
            }
            catch
            {
                GaussDBConnection.Close();
            }
        }
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override async Task CreateTablesAsync(CancellationToken cancellationToken = default)
    {
        var designTimeModel = Dependencies.CurrentContext.Context.GetService<IDesignTimeModel>().Model;
        var operations = Dependencies.ModelDiffer.GetDifferences(null, designTimeModel.GetRelationalModel());
        var commands = Dependencies.MigrationsSqlGenerator.Generate(operations, designTimeModel);

        try
        {
            await Dependencies.MigrationCommandExecutor.ExecuteNonQueryAsync(commands, _connection, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (PostgresException e) when (e is { SqlState: "23505", ConstraintName: "pg_type_typname_nsp_index" })
        {
            // This occurs when two connections are trying to create the same database concurrently
            // (happens in the tests). Simply ignore the error.
        }

        // If a PostgreSQL extension, enum or range was added, we want GaussDB to reload all types at the ADO.NET level.
        var reloadTypes = operations
            .OfType<AlterDatabaseOperation>()
            .Any(o => o.GetPostgresExtensions().Any() || o.GetPostgresEnums().Any() || o.GetPostgresRanges().Any());

        if (reloadTypes && _connection.DbConnection is GaussDBConnection GaussDBConnection)
        {
            await GaussDBConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await GaussDBConnection.ReloadTypesAsync().ConfigureAwait(false);
            }
            catch
            {
                await GaussDBConnection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private IReadOnlyList<MigrationCommand> CreateDropCommands()
    {
        var operations = new MigrationOperation[]
        {
            // TODO Check DbConnection.Database always gives us what we want
            // Issue #775
            new GaussDBDropDatabaseOperation { Name = _connection.DbConnection.Database }
        };

        return Dependencies.MigrationsSqlGenerator.Generate(operations);
    }

    // Clear connection pools in case there are active connections that are pooled
    private static void ClearAllPools()
        => GaussDBConnection.ClearAllPools();

    // Clear connection pool for the database connection since after the 'create database' call, a previously
    // invalid connection may now be valid.
    private void ClearPool()
        => GaussDBConnection.ClearPool((GaussDBConnection)_connection.DbConnection);
}
