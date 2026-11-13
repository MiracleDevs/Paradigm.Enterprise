using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Data.MySql.Context;
using Paradigm.Enterprise.Data.MySql.Extensions;
using Paradigm.Enterprise.Data.MySql.StoredProcedures;
using Paradigm.Enterprise.Data.MySql.StoredProcedures.Mappers;
using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Data.StoredProcedures.Mappers;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Services;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Interfaces;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.MySql.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class MySqlIntegrationTests
{
    #region Nested Types

    private sealed class AuditRecord : EntityBase<int>, IAuditableEntity<DateTime, int>
    {
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid Token { get; set; }
        public bool IsActive { get; set; }
        public DateTime OccurredAt { get; set; }
        public byte[] Payload { get; set; } = [];
        public int CategoryId { get; set; } = 1;
        public int? CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public int? ModifiedByUserId { get; set; }
        public DateTime? ModificationDate { get; set; }
    }

    private sealed class GuidRecord : EntityBase<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestUser : EntityBase<int>;

    private sealed class CatalogContext(IServiceProvider services, DbContextOptions<CatalogContext> options)
        : DbContextBase<int>(services, options)
    {
        #region Overrides

        protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);

        #endregion
    }

    private sealed class SecondContext(IServiceProvider services, DbContextOptions<SecondContext> options)
        : DbContextBase<int>(services, options)
    {
        #region Overrides

        protected override void OnModelCreating(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);

        #endregion
    }

    private sealed class GuidContext(IServiceProvider services, DbContextOptions<GuidContext> options)
        : DbContextBase<Guid>(services, options)
    {
        #region Overrides

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GuidRecord>().ToTable("GuidRecord");
            modelBuilder.Entity<GuidRecord>().Property(value => value.Id).HasColumnType("char(36)").ValueGeneratedNever();
            modelBuilder.Entity<GuidRecord>().Property(value => value.Name).HasMaxLength(100);
        }

        #endregion
    }

    private sealed class AuditRepository(IServiceProvider services) : EditRepositoryBase<AuditRecord, CatalogContext, int>(services)
    {
        #region Overrides

        protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<AuditRecord>)>> GetSearchPaginatedFunction(PaginationParametersBase parameters) =>
            async input =>
            {
                var result = await new SearchProcedure().ExecuteAsync(GetDbConnection(), (SearchParameters)input, UnitOfWork);
                return (result.Item1!, result.Item2!);
            };

        #endregion
    }

    private sealed class GuidRepository(IServiceProvider services) : EditRepositoryBase<GuidRecord, GuidContext, Guid>(services);

    private sealed class SearchParameters : PaginationParametersBase;

    private sealed class SearchParameterMapper : MySqlParameterMapperBase
    {
        #region Overrides

        protected override void AddMySqlParameters(object parameters)
        {
            var search = (SearchParameters)parameters;
            AddMySqlParameter("@PageSize", search.PageSize);
            AddMySqlParameter("@PageNumber", search.PageNumber);
        }

        #endregion
    }

    private sealed record InsertParameters(string Name);

    private sealed class InsertParameterMapper : MySqlParameterMapperBase
    {
        #region Overrides

        protected override void AddMySqlParameters(object parameters) => AddMySqlParameter("@Name", ((InsertParameters)parameters).Name);

        #endregion
    }

    private sealed class InsertProcedure : StoredProcedureBase<InsertParameters>
    {
        #region Overrides

        protected override string StoredProcedureName => "InsertRecord";

        #endregion
    }

    private sealed class SearchProcedure : ResultStoredProcedureBase<SearchParameters, PaginationInfo, List<AuditRecord>>
    {
        #region Overrides

        protected override string StoredProcedureName => "SearchRecords";

        #endregion
    }

    private sealed class ScalarProcedure(string name = "SingleResult") : ResultStoredProcedureBase<object?, long>
    {
        #region Overrides

        protected override string StoredProcedureName => name;

        #endregion
    }

    private sealed class EmptyMiddleProcedure : ResultStoredProcedureBase<object?, long, long, long>
    {
        #region Overrides

        protected override string StoredProcedureName => "EmptyMiddle";

        #endregion
    }

    private sealed class TimedScalarProcedure : ResultStoredProcedureBase<object?, long>
    {
        #region Overrides

        protected override string StoredProcedureName => "SingleResult";

        protected override int? ExecutionTimeout => 1;

        #endregion
    }

    private sealed class LongDeadlineProcedure : ResultStoredProcedureBase<object?, long>
    {
        #region Overrides

        protected override string StoredProcedureName => "SingleResult";

        protected override int? ExecutionTimeout => int.MaxValue;

        #endregion
    }

    private sealed class SlowReadingProcedure : StoredProcedureBase
    {
        #region Public Methods

        public Task<int> ReadSlowlyAsync(DbConnection connection) => ExecuteAsync(connection, null, async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            return 1;
        });

        #endregion

        #region Overrides

        protected override string StoredProcedureName => "SingleResult";

        protected override int? ExecutionTimeout => 1;

        #endregion
    }

    private sealed class SixteenResultsProcedure : ResultStoredProcedureBase<object?, long, long, long, long, long, long, long, long, long, long, long, long, long, long, long, long>
    {
        #region Overrides

        protected override string StoredProcedureName => "SixteenResults";

        #endregion
    }

    private sealed class SlowProcedure : StoredProcedureBase
    {
        #region Overrides

        protected override string StoredProcedureName => "SlowResult";

        protected override int? ExecutionTimeout => 1;

        #endregion
    }

    private sealed class MissingProcedure : StoredProcedureBase
    {
        #region Overrides

        protected override string StoredProcedureName => "DoesNotExist";

        #endregion
    }

    private sealed class UpdateAmountProcedure : StoredProcedureBase
    {
        #region Overrides

        protected override string StoredProcedureName => "UpdateAmount";

        protected override int? ExecutionTimeout => 10;

        #endregion
    }

    private sealed record BrokenResult;

    private sealed record SlowMappedResult(long Value);

    private sealed class SlowResultMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader)
        {
            // Deliberately block this synchronous mapper to exercise the elapsed-deadline guard.
            Thread.Sleep(1200);
            return new SlowMappedResult(Convert.ToInt64(reader[0]));
        }

        #endregion
    }

    private sealed class SlowMappingProcedure : ResultStoredProcedureBase<object?, SlowMappedResult>
    {
        #region Overrides

        protected override string StoredProcedureName => "SingleResult";

        protected override int? ExecutionTimeout => 1;

        #endregion
    }

    private sealed class BrokenResultMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader) => throw new FormatException("synthetic mapping failure");

        #endregion
    }

    private sealed class BrokenMappingProcedure : ResultStoredProcedureBase<object?, BrokenResult>
    {
        #region Overrides

        protected override string StoredProcedureName => "SingleResult";

        #endregion
    }

    private sealed class PaginationMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader) => new PaginationInfo
        {
            ItemsCount = Convert.ToInt32(reader["ItemsCount"]),
            TotalPages = Convert.ToInt32(reader["TotalPages"]),
            PageNumber = Convert.ToInt32(reader["PageNumber"])
        };

        #endregion
    }

    private sealed class AuditRecordMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader) => new AuditRecord
        {
            Id = Convert.ToInt32(reader["Id"]),
            Name = Convert.ToString(reader["Name"])!,
            Amount = Convert.ToDecimal(reader["Amount"]),
            Token = Guid.Parse(Convert.ToString(reader["Token"])!),
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            OccurredAt = DateTime.SpecifyKind(Convert.ToDateTime(reader["OccurredAt"]), DateTimeKind.Utc),
            Payload = (byte[])reader["Payload"]
        };

        #endregion
    }

    #endregion

    #region Fields

    private string? _adminConnectionString;
    private string? _databaseName;
    private string _connectionString = string.Empty;

    #endregion

    #region Public Methods

    [TestInitialize]
    public async Task CreateIsolatedDatabaseAsync()
    {
        var configured = Environment.GetEnvironmentVariable("PARADIGM_MYSQL_TEST_CONNECTION_STRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(configured),
            "Integration tests require PARADIGM_MYSQL_TEST_CONNECTION_STRING targeting a disposable MySQL 8.4 server with CREATE/DROP DATABASE privileges.");
        var admin = new MySqlConnectionStringBuilder(configured) { Database = string.Empty, ConnectionTimeout = 10, DefaultCommandTimeout = 15, OldGuids = false };
        _adminConnectionString = admin.ConnectionString;
        await using var connection = new MySqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using (var version = connection.CreateCommand())
        {
            version.CommandText = "SELECT VERSION()";
            var actual = Convert.ToString(await version.ExecuteScalarAsync());
            Assert.IsTrue(actual?.StartsWith("8.4.", StringComparison.Ordinal) == true, "This suite certifies MySQL 8.4 only.");
        }

        _databaseName = "mdpe12_" + Guid.NewGuid().ToString("N");
        await using (var create = connection.CreateCommand())
        {
            create.CommandText = $"CREATE DATABASE `{_databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci";
            await create.ExecuteNonQueryAsync();
        }
        admin.Database = _databaseName;
        _connectionString = admin.ConnectionString;
        await CreateSchemaAsync();
        MySqlParameterMapperFactory.RegisterMapper<InsertParameters>(() => new InsertParameterMapper());
        MySqlParameterMapperFactory.RegisterMapper<SearchParameters>(() => new SearchParameterMapper());
        DataReaderMapperFactory.RegisterMapper<PaginationInfo>(() => new PaginationMapper());
        DataReaderMapperFactory.RegisterMapper<AuditRecord>(() => new AuditRecordMapper());
        DataReaderMapperFactory.RegisterMapper<BrokenResult>(() => new BrokenResultMapper());
        DataReaderMapperFactory.RegisterMapper<SlowMappedResult>(() => new SlowResultMapper());
    }

    [TestCleanup]
    public async Task DropOwnedDatabaseAsync()
    {
        if (_databaseName is null || _adminConnectionString is null)
            return;
        Assert.IsTrue(_databaseName.StartsWith("mdpe12_", StringComparison.Ordinal) && _databaseName.Length == 39);
        await using var connection = new MySqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS `{_databaseName}`";
        await command.ExecuteNonQueryAsync();
    }

    [TestMethod]
    public async Task Shared_repository_crud_audits_and_round_trips_explicit_value_mappings()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<ILoggedUserService<int>>().Authenticate(new TestUser { Id = 42 });
        var item = NewRecord("Café 🎉");

        await repository.AddAsync(item);
        Assert.AreEqual(0L, await CountRecordsAsync());
        await unitOfWork.CommitChangesAsync();
        Assert.IsTrue(item.Id > 0);
        Assert.AreEqual(42, item.CreatedByUserId);
        Assert.AreEqual(DateTimeKind.Utc, item.CreationDate.Kind);
        context.ChangeTracker.Clear();
        var loaded = await repository.GetByIdAsync(item.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(item.Name, loaded.Name);
        Assert.AreEqual(item.Amount, loaded.Amount);
        Assert.AreEqual(item.Token, loaded.Token);
        Assert.AreEqual(item.OccurredAt, loaded.OccurredAt);
        Assert.IsTrue(loaded.IsActive);
        Assert.IsNull(loaded.ModificationDate);
        CollectionAssert.AreEqual(item.Payload, loaded.Payload);

        loaded.Name = "renamed";
        await repository.UpdateAsync(loaded);
        await unitOfWork.CommitChangesAsync();
        Assert.AreEqual(42, loaded.ModifiedByUserId);
        Assert.IsNotNull(loaded.ModificationDate);
        Assert.HasCount(1, (await repository.GetByIdsAsync([item.Id])).ToList());

        await repository.DeleteAsync(loaded);
        await unitOfWork.CommitChangesAsync();
        Assert.IsNull(await repository.GetByIdAsync(item.Id));
    }

    [TestMethod]
    public async Task Guid_identifier_uses_explicit_char36_mapping_with_existing_repository()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<GuidRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var item = new GuidRecord { Id = Guid.NewGuid(), Name = "guid-id" };

        await repository.AddAsync(item);
        await unitOfWork.CommitChangesAsync();
        scope.ServiceProvider.GetRequiredService<GuidContext>().ChangeTracker.Clear();

        var loaded = await repository.GetByIdAsync(item.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(item.Id, loaded.Id);
        Assert.AreEqual(item.Name, loaded.Name);
    }

    [TestMethod]
    public async Task Pagination_maps_metadata_before_rows_and_preserves_empty_pages()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repository.AddAsync(new[] { NewRecord("first"), NewRecord("second"), NewRecord("third") });
        await unitOfWork.CommitChangesAsync();

        var page = await repository.SearchAsync(new SearchParameters { PageSize = 2, PageNumber = 2 });
        Assert.AreEqual(3, page.PageInfo.ItemsCount);
        Assert.AreEqual(2, page.PageInfo.TotalPages);
        Assert.AreEqual(2, page.PageInfo.PageNumber);
        CollectionAssert.AreEqual(new[] { "third" }, page.Results.Select(value => value.Name).ToArray());

        var empty = await repository.SearchAsync(new SearchParameters { PageSize = 2, PageNumber = 4 });
        Assert.AreEqual(3, empty.PageInfo.ItemsCount);
        Assert.HasCount(0, empty.Results.ToList());
    }

    [TestMethod]
    public async Task Two_contexts_and_procedure_writes_commit_or_rollback_through_shared_unit_of_work()
    {
        foreach (var commit in new[] { false, true })
        {
            using var services = CreateServices();
            using var scope = services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
            var first = scope.ServiceProvider.GetRequiredService<CatalogContext>();
            var second = scope.ServiceProvider.GetRequiredService<SecondContext>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.RegisterCommiteable(second);
            var connection = (MySqlConnection)first.Database.GetDbConnection();
            Assert.AreSame(connection, second.Database.GetDbConnection());

            using (var transaction = unitOfWork.CreateTransaction())
            {
                await repository.AddAsync(NewRecord("first-" + commit));
                second.Set<AuditRecord>().Add(NewRecord("second-" + commit));
                await unitOfWork.CommitChangesAsync();
                await new InsertProcedure().ExecuteAsync(connection, new InsertParameters("routine-" + commit), unitOfWork);
                Assert.AreEqual(ConnectionState.Open, connection.State);
                if (commit)
                    transaction.Commit();
                else
                    transaction.Rollback();
            }
            Assert.AreEqual(commit ? 3L : 0L, await CountRecordsAsync());
        }
    }

    [TestMethod]
    public async Task Routine_rejects_a_different_connection_without_leaving_the_original_transaction()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var transaction = unitOfWork.CreateTransaction();
        await using var differentConnection = new MySqlConnection(_connectionString);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => new InsertProcedure()
            .ExecuteAsync(differentConnection, new InsertParameters("must-not-insert"), unitOfWork));

        Assert.IsTrue(transaction.IsActive);
        Assert.AreEqual(ConnectionState.Open, context.Database.GetDbConnection().State);
        transaction.Rollback();
        Assert.AreEqual(0L, await CountRecordsAsync());
    }

    [TestMethod]
    public async Task One_empty_middle_and_sixteen_results_preserve_positions_and_allow_connection_reuse()
    {
        await using var connection = new MySqlConnection(_connectionString);
        Assert.AreEqual(1L, await new ScalarProcedure().ExecuteAsync(connection, null));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        var empty = await new EmptyMiddleProcedure().ExecuteAsync(connection, null);
        Assert.AreEqual(1L, empty.Item1);
        Assert.AreEqual(0L, empty.Item2);
        Assert.AreEqual(3L, empty.Item3);

        var results = await new SixteenResultsProcedure().ExecuteAsync(connection, null);
        ITuple tuple = results;
        Assert.AreEqual(16, tuple.Length);
        for (var i = 0; i < tuple.Length; i++)
            Assert.AreEqual((long)i + 1, tuple[i]);
        await connection.OpenAsync();
        Assert.AreEqual(1L, await new ScalarProcedure().ExecuteAsync(connection, null));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task Mapping_command_and_timeout_failures_close_nontransactional_connections()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        var mappingFailure = await Assert.ThrowsExactlyAsync<FormatException>(() => new BrokenMappingProcedure().ExecuteAsync(connection, null));
        Assert.AreEqual("synthetic mapping failure", mappingFailure.Message);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        await Assert.ThrowsExactlyAsync<MySqlException>(() => new MissingProcedure().ExecuteAsync(connection));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        var timeoutWatch = Stopwatch.StartNew();
        await Assert.ThrowsExactlyAsync<TimeoutException>(() => new SlowProcedure().ExecuteAsync(connection));
        Assert.IsTrue(timeoutWatch.Elapsed < TimeSpan.FromSeconds(4), "A one-second timeout must interrupt the five-second routine before it completes.");
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1L, await new ScalarProcedure().ExecuteAsync(connection, null));
    }

    [TestMethod]
    [Timeout(15000)]
    public async Task Deadline_includes_result_consumption_and_preserves_connection_reuse()
    {
        await using var connection = new MySqlConnection(_connectionString);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => new SlowReadingProcedure().ReadSlowlyAsync(connection));

        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1L, await new ScalarProcedure().ExecuteAsync(connection, null));
    }

    [TestMethod]
    [Timeout(15000)]
    public async Task Synchronous_mapper_cannot_return_a_successful_result_after_the_deadline()
    {
        await using var connection = new MySqlConnection(_connectionString);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => new SlowMappingProcedure().ExecuteAsync(connection, null));

        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1L, await new ScalarProcedure().ExecuteAsync(connection, null));
    }

    [TestMethod]
    [Timeout(15000)]
    public async Task Successful_explicit_deadline_is_stopped_before_later_commands()
    {
        await using var connection = new MySqlConnection(_connectionString);
        Assert.AreEqual(1L, await new TimedScalarProcedure().ExecuteAsync(connection, null));

        Assert.AreEqual(0L, await new ScalarProcedure("WaitAndReturn").ExecuteAsync(connection, null),
            "An expired watchdog from the previous command must not interrupt connection reuse.");
        Assert.AreEqual(1L, await new TimedScalarProcedure().ExecuteAsync(connection, null));
        await using var otherConnection = new MySqlConnection(_connectionString);
        Assert.AreEqual(0L, await new ScalarProcedure("WaitAndReturn").ExecuteAsync(otherConnection, null),
            "An expired watchdog must not interrupt another connection borrowing the pooled session.");
    }

    [TestMethod]
    [Timeout(15000)]
    public async Task Large_positive_deadline_accepts_the_existing_integer_timeout_range()
    {
        await using var connection = new MySqlConnection(_connectionString);

        Assert.AreEqual(1L, await new LongDeadlineProcedure().ExecuteAsync(connection, null));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1L, await new ScalarProcedure().ExecuteAsync(connection, null));
    }

    [TestMethod]
    public async Task Constraint_errors_propagate_and_failed_transaction_remains_caller_owned()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await new InsertProcedure().ExecuteAsync(connection, new InsertParameters("unique"));
        var duplicate = await Assert.ThrowsExactlyAsync<MySqlException>(() => new InsertProcedure().ExecuteAsync(connection, new InsertParameters("unique")));
        Assert.AreEqual(1062, duplicate.Number);
        Assert.AreEqual(ConnectionState.Closed, connection.State);

        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var transactionConnection = scope.ServiceProvider.GetRequiredService<CatalogContext>().Database.GetDbConnection();
        using var transaction = unitOfWork.CreateTransaction();
        await repository.AddAsync(NewRecord("pending"));
        await unitOfWork.CommitChangesAsync();
        await Assert.ThrowsExactlyAsync<MySqlException>(() => new InsertProcedure().ExecuteAsync(transactionConnection, new InsertParameters("unique"), unitOfWork));
        Assert.IsTrue(transaction.IsActive);
        Assert.AreEqual(ConnectionState.Open, transactionConnection.State);
        transaction.Rollback();
        Assert.AreEqual(1L, await CountRecordsAsync());
    }

    [TestMethod]
    public async Task Scope_disposal_releases_the_connection_but_context_disposal_does_not()
    {
        using var services = CreateServices();
        MySqlConnection connection;
        using (var scope = services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
            connection = (MySqlConnection)context.Database.GetDbConnection();
            await connection.OpenAsync();
            context.Dispose();
            Assert.AreEqual(ConnectionState.Open, connection.State);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            Assert.AreEqual(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        }

        Assert.AreEqual(ConnectionState.Closed, connection.State);
        var disposed = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => connection.OpenAsync());
        StringAssert.Contains(disposed.Message, "disposed");
    }

    [TestMethod]
    public async Task Provider_disposal_attempts_every_owned_connection_and_preserves_the_first_failure()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:First"] = _connectionString,
            ["ConnectionStrings:Second"] = _connectionString
        }).Build();
        var provider = new MySqlDbConnectionProvider(configuration);
        var first = provider["First"];
        var second = provider["Second"];
        var expectedFailure = new InvalidOperationException("synthetic connection close failure");
        StateChangeEventHandler failFirstClose = (_, change) =>
        {
            if (change.OriginalState == ConnectionState.Open && change.CurrentState == ConnectionState.Closed)
                throw expectedFailure;
        };
        first.StateChange += failFirstClose;
        try
        {
            await first.OpenAsync();
            await second.OpenAsync();

            var actualFailure = Assert.ThrowsExactly<InvalidOperationException>(provider.Dispose);

            Assert.AreSame(expectedFailure, actualFailure);
            Assert.AreEqual(ConnectionState.Closed, second.State);
            Assert.ThrowsExactly<ObjectDisposedException>(() => _ = provider["Second"]);
            provider.Dispose();
        }
        finally
        {
            first.StateChange -= failFirstClose;
            first.Dispose();
            second.Dispose();
            provider.Dispose();
        }
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task Lock_wait_and_foreign_key_errors_are_propagated_without_implicit_retries()
    {
        await using var lockConnection = new MySqlConnection(_connectionString);
        await lockConnection.OpenAsync();
        await new InsertProcedure().ExecuteAsync(lockConnection, new InsertParameters("locked"));
        await lockConnection.OpenAsync();
        await using var transaction = await lockConnection.BeginTransactionAsync();
        await using var lockingCommand = lockConnection.CreateCommand();
        lockingCommand.Transaction = (MySqlTransaction)transaction;
        lockingCommand.CommandText = "UPDATE AuditRecord SET Amount = 2 WHERE Name = 'locked'";
        await lockingCommand.ExecuteNonQueryAsync();

        await using var contender = new MySqlConnection(_connectionString);
        await contender.OpenAsync();
        await using (var configure = contender.CreateCommand())
        {
            configure.CommandText = "SET SESSION innodb_lock_wait_timeout = 1";
            await configure.ExecuteNonQueryAsync();
        }
        var lockFailure = await Assert.ThrowsExactlyAsync<MySqlException>(() => new UpdateAmountProcedure().ExecuteAsync(contender));
        Assert.AreEqual(1205, lockFailure.Number);
        Assert.AreEqual(ConnectionState.Closed, contender.State);
        await transaction.RollbackAsync();

        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var invalid = NewRecord("invalid-fk");
        invalid.CategoryId = 999;
        await repository.AddAsync(invalid);
        var foreignKeyFailure = await Assert.ThrowsExactlyAsync<DbUpdateException>(() => scope.ServiceProvider
            .GetRequiredService<IUnitOfWork>().CommitChangesAsync());
        Assert.IsInstanceOfType<MySqlException>(foreignKeyFailure.InnerException);
        Assert.AreEqual(1452, ((MySqlException)foreignKeyFailure.InnerException).Number);
    }

    #endregion

    #region Private Methods

    private ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Integration"] = _connectionString }).Build());
        services.AddScoped<MySqlDbConnectionProvider>();
        services.RegisterContext<CatalogContext>("Integration");
        services.RegisterContext<SecondContext>("Integration");
        services.RegisterContext<GuidContext>("Integration");
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.RegisterLoggedUserService();
        services.AddScoped<AuditRepository>();
        services.AddScoped<GuidRepository>();
        return services.BuildServiceProvider();
    }

    private static AuditRecord NewRecord(string name) => new()
    {
        Name = name,
        Amount = 1234.5678m,
        Token = Guid.NewGuid(),
        IsActive = true,
        OccurredAt = new DateTime(2026, 11, 13, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234560),
        Payload = [0, 1, 128, 255],
        CreationDate = DateTime.UtcNow
    };

    private static void ConfigureModel(ModelBuilder modelBuilder)
    {
        var record = modelBuilder.Entity<AuditRecord>();
        record.ToTable("AuditRecord");
        record.Property(value => value.Id).ValueGeneratedOnAdd();
        record.Property(value => value.Name).HasMaxLength(100);
        record.Property(value => value.Amount).HasPrecision(18, 4);
        record.Property(value => value.Token).HasColumnType("char(36)");
        record.Property(value => value.IsActive).HasColumnType("tinyint(1)");
        record.Property(value => value.OccurredAt).HasColumnType("datetime(6)")
            .HasConversion(value => value.ToUniversalTime(), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        record.Property(value => value.CreationDate).HasColumnType("datetime(6)");
        record.Property(value => value.ModificationDate).HasColumnType("datetime(6)");
        record.Property(value => value.Payload).HasColumnType("varbinary(100)");
    }

    private async Task<long> CountRecordsAsync()
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM AuditRecord";
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private async Task ExecuteFixtureSqlAsync(string sql)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateSchemaAsync()
    {
        await ExecuteFixtureSqlAsync("CREATE TABLE Category (Id INT PRIMARY KEY) ENGINE=InnoDB");
        await ExecuteFixtureSqlAsync("INSERT INTO Category VALUES (1)");
        await ExecuteFixtureSqlAsync("""
            CREATE TABLE AuditRecord (
                Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Name VARCHAR(100) NOT NULL UNIQUE,
                Amount DECIMAL(18,4) NOT NULL,
                Token CHAR(36) NOT NULL,
                IsActive TINYINT(1) NOT NULL,
                OccurredAt DATETIME(6) NOT NULL,
                Payload VARBINARY(100) NOT NULL,
                CategoryId INT NOT NULL DEFAULT 1,
                CreatedByUserId INT NULL,
                CreationDate DATETIME(6) NOT NULL,
                ModifiedByUserId INT NULL,
                ModificationDate DATETIME(6) NULL,
                CONSTRAINT FK_AuditRecord_Category FOREIGN KEY (CategoryId) REFERENCES Category(Id)
            ) ENGINE=InnoDB
            """);
        await ExecuteFixtureSqlAsync("CREATE TABLE GuidRecord (Id CHAR(36) PRIMARY KEY, Name VARCHAR(100) NOT NULL) ENGINE=InnoDB");
        await ExecuteFixtureSqlAsync("""
            CREATE PROCEDURE InsertRecord(IN Name VARCHAR(100))
            INSERT INTO AuditRecord (Name, Amount, Token, IsActive, OccurredAt, Payload, CreationDate)
            VALUES (Name, 1.0000, '00000000-0000-0000-0000-000000000001', 1, UTC_TIMESTAMP(6), X'01', UTC_TIMESTAMP(6))
            """);
        await ExecuteFixtureSqlAsync("""
            CREATE PROCEDURE SearchRecords(IN PageSize INT, IN PageNumber INT)
            BEGIN
                DECLARE PageOffset INT;
                SET PageOffset = (PageNumber - 1) * PageSize;
                SELECT COUNT(*) AS ItemsCount, CAST(CEILING(COUNT(*) / PageSize) AS SIGNED) AS TotalPages, PageNumber AS PageNumber FROM AuditRecord;
                SELECT Id, Name, Amount, Token, IsActive, OccurredAt, Payload FROM AuditRecord ORDER BY Id LIMIT PageSize OFFSET PageOffset;
            END
            """);
        await ExecuteFixtureSqlAsync("CREATE PROCEDURE SingleResult() SELECT CAST(1 AS SIGNED) AS Value");
        await ExecuteFixtureSqlAsync("CREATE PROCEDURE EmptyMiddle() BEGIN SELECT CAST(1 AS SIGNED) AS Value; SELECT CAST(2 AS SIGNED) AS Value WHERE FALSE; SELECT CAST(3 AS SIGNED) AS Value; END");
        await ExecuteFixtureSqlAsync("CREATE PROCEDURE SixteenResults() BEGIN " + string.Join(" ", Enumerable.Range(1, 16).Select(number => $"SELECT CAST({number} AS SIGNED) AS Value;")) + " END");
        await ExecuteFixtureSqlAsync("CREATE PROCEDURE SlowResult() SELECT SLEEP(5)");
        await ExecuteFixtureSqlAsync("CREATE PROCEDURE WaitAndReturn() SELECT CAST(SLEEP(2) AS SIGNED) AS Value");
        await ExecuteFixtureSqlAsync("CREATE PROCEDURE UpdateAmount() UPDATE AuditRecord SET Amount = 3 WHERE Name = 'locked'");
    }

    #endregion
}