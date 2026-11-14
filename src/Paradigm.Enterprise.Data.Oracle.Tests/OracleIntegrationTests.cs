using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Data.Oracle.Context;
using Paradigm.Enterprise.Data.Oracle.Extensions;
using Paradigm.Enterprise.Data.Oracle.StoredProcedures;
using Paradigm.Enterprise.Data.Oracle.StoredProcedures.Mappers;
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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Data.Oracle.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class OracleIntegrationTests
{
    #region Nested Types

    private sealed class AuditRecord : EntityBase<int>, IAuditableEntity<DateTime, int>
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
        public decimal Amount { get; set; }
        public Guid Token { get; set; }
        public bool IsActive { get; set; }
        public DateTime OccurredAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreationDate { get; set; }
        public int? ModifiedByUserId { get; set; }
        public DateTime? ModificationDate { get; set; }
    }

    private sealed class TestUser : EntityBase<int>;

    private sealed class GuidRecord : EntityBase<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class GuidContext(IServiceProvider services, DbContextOptions<GuidContext> options)
        : DbContextBase<Guid>(services, options)
    {
        #region Overrides

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var record = builder.Entity<GuidRecord>();
            record.ToTable("GuidRecord");
            record.Property(item => item.Id).HasColumnType("RAW(16)").ValueGeneratedNever();
            record.Property(item => item.Name).HasColumnType("NVARCHAR2(100)");
        }

        #endregion
    }

    private sealed class GuidRepository(IServiceProvider services) : EditRepositoryBase<GuidRecord, GuidContext, Guid>(services);

    private sealed class CatalogContext(IServiceProvider services, DbContextOptions<CatalogContext> options)
        : DbContextBase<int>(services, options)
    {
        #region Overrides

        protected override void OnModelCreating(ModelBuilder builder) => ConfigureModel(builder);

        #endregion
    }

    private sealed class SecondContext(IServiceProvider services, DbContextOptions<SecondContext> options)
        : DbContextBase<int>(services, options)
    {
        #region Overrides

        protected override void OnModelCreating(ModelBuilder builder) => ConfigureModel(builder);

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

    private sealed class SearchParameters : PaginationParametersBase;
    private sealed record InsertParameters(string Name, decimal Amount = 1.25m);
    private sealed record ScalarRow(decimal Value);
    private sealed record BrokenRow;

    private sealed class InsertMapper : OracleParameterMapperBase
    {
        #region Overrides

        protected override void AddOracleParameters(object parameters)
        {
            var input = (InsertParameters)parameters;
            // Deliberately reverse the declaration order to prove BindByName.
            AddOracleParameter(new OracleParameter("p_amount", OracleDbType.Decimal) { Value = input.Amount, Precision = 18, Scale = 4 });
            AddOracleParameter("p_name", input.Name);
        }

        #endregion
    }

    private sealed class SearchMapper : OracleParameterMapperBase
    {
        #region Overrides

        protected override void AddOracleParameters(object parameters)
        {
            var input = (SearchParameters)parameters;
            AddOracleParameter("p_page_size", input.PageSize);
            AddOracleParameter("p_page_number", input.PageNumber);
        }

        #endregion
    }

    private sealed class ScalarMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader) => new ScalarRow(Convert.ToDecimal(reader.GetValue(0)));

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

    private sealed class RecordMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader) => new AuditRecord
        {
            Id = Convert.ToInt32(reader["Id"]),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Amount = Convert.ToDecimal(reader["Amount"]),
            Token = new Guid((byte[])reader["Token"]),
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            OccurredAt = DateTime.SpecifyKind(reader.GetDateTime(reader.GetOrdinal("OccurredAt")), DateTimeKind.Utc)
        };

        #endregion
    }

    private sealed class BrokenMapper : IDataReaderMapper
    {
        #region Public Methods

        public object Map(IDataReader reader) => throw new FormatException("synthetic mapping failure");

        #endregion
    }

    private sealed class InsertProcedure : StoredProcedureBase<InsertParameters>
    {
        #region Overrides

        protected override string StoredProcedureName => "InsertRecord";

        #endregion
    }

    private sealed class NoParameterProcedure(string name = "NoParameters", int? timeout = null) : StoredProcedureBase
    {
        #region Overrides

        protected override string StoredProcedureName => name;
        protected override int? ExecutionTimeout => timeout;

        #endregion
    }

    private sealed class ScalarProcedure(string name = "SingleResult", int? timeout = null) : ResultStoredProcedureBase<object?, ScalarRow>
    {
        #region Overrides

        protected override string StoredProcedureName => name;
        protected override int? ExecutionTimeout => timeout;

        #endregion
    }

    private sealed class CustomCursorProcedure : ResultStoredProcedureBase<object?, ScalarRow>
    {
        #region Overrides

        protected override string StoredProcedureName => "CustomCursor";
        protected override string GetResultParameterName(int resultIndex) => "p_rows";

        #endregion
    }

    private sealed class PrimitiveProcedure<T>(string name = "SingleResult") : ResultStoredProcedureBase<object?, T>
    {
        #region Overrides

        protected override string StoredProcedureName => name;

        #endregion
    }

    private sealed class SearchProcedure : ResultStoredProcedureBase<SearchParameters, PaginationInfo, List<AuditRecord>>
    {
        #region Overrides

        protected override string StoredProcedureName => "SearchRecords";

        #endregion
    }

    private sealed class EmptyMiddleProcedure : ResultStoredProcedureBase<object?, ScalarRow, ScalarRow, ScalarRow>
    {
        #region Overrides

        protected override string StoredProcedureName => "EmptyMiddle";

        #endregion
    }

    private sealed class SixteenProcedure : ResultStoredProcedureBase<object?, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow, ScalarRow>
    {
        #region Overrides

        protected override string StoredProcedureName => "SixteenResults";

        #endregion
    }

    private sealed class BrokenProcedure : ResultStoredProcedureBase<object?, BrokenRow>
    {
        #region Overrides

        protected override string StoredProcedureName => "SingleResult";

        #endregion
    }

    #endregion

    #region Fields

    private string _adminConnectionString = string.Empty;
    private string _connectionString = string.Empty;
    private string? _ownedUser;

    #endregion

    #region Public Methods

    [TestInitialize]
    public async Task CreateOwnedSchemaAsync()
    {
        var configured = Environment.GetEnvironmentVariable("PARADIGM_ORACLE_TEST_CONNECTION_STRING");
        Assert.IsFalse(string.IsNullOrWhiteSpace(configured),
            "Oracle integration tests require PARADIGM_ORACLE_TEST_CONNECTION_STRING targeting a disposable Oracle Free PDB with CREATE/DROP USER and GRANT privileges; application configuration is never used.");
        var admin = new OracleConnectionStringBuilder(configured) { Pooling = false, ConnectionTimeout = 10 };
        _adminConnectionString = admin.ConnectionString;
        await using var connection = new OracleConnection(_adminConnectionString);
        await connection.OpenAsync();
        Assert.IsTrue(connection.ServerVersion.StartsWith("23.", StringComparison.Ordinal), "This suite certifies Oracle Database Free 23ai/26ai (23.x) only.");
        var name = "PARADIGM_" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        var password = "Fixture_" + Guid.NewGuid().ToString("N")[..20];
        await using var defaultTablespace = connection.CreateCommand();
        defaultTablespace.CommandText = "SELECT PROPERTY_VALUE FROM DATABASE_PROPERTIES WHERE PROPERTY_NAME = 'DEFAULT_PERMANENT_TABLESPACE'";
        var tablespace = Convert.ToString(await defaultTablespace.ExecuteScalarAsync());
        Assert.IsFalse(string.IsNullOrWhiteSpace(tablespace), "A default permanent tablespace is required for the disposable fixture schema.");
        var quotedTablespace = "\"" + tablespace.Replace("\"", "\"\"") + "\"";
        await ExecuteOnAsync(connection, $"CREATE USER {name} IDENTIFIED BY \"{password}\" DEFAULT TABLESPACE {quotedTablespace} QUOTA 20M ON {quotedTablespace}");
        _ownedUser = name;
        await ExecuteOnAsync(connection, $"GRANT CREATE SESSION, CREATE TABLE, CREATE PROCEDURE, CREATE SEQUENCE TO {name}");
        admin.UserID = name;
        admin.Password = password;
        _connectionString = admin.ConnectionString;
        await CreateSchemaAsync();
        OracleParameterMapperFactory.RegisterMapper<InsertParameters>(() => new InsertMapper());
        OracleParameterMapperFactory.RegisterMapper<SearchParameters>(() => new SearchMapper());
        DataReaderMapperFactory.RegisterMapper<ScalarRow>(() => new ScalarMapper());
        DataReaderMapperFactory.RegisterMapper<PaginationInfo>(() => new PaginationMapper());
        DataReaderMapperFactory.RegisterMapper<AuditRecord>(() => new RecordMapper());
        DataReaderMapperFactory.RegisterMapper<BrokenRow>(() => new BrokenMapper());
    }

    [TestCleanup]
    public async Task DropOwnedSchemaAsync()
    {
        if (_ownedUser is null)
            return;
        Assert.IsTrue(_ownedUser.StartsWith("PARADIGM_", StringComparison.Ordinal) && _ownedUser.Length == 41 &&
            _ownedUser[9..].All(Uri.IsHexDigit), "Only the exact generated fixture schema may be dropped.");
        await using var connection = new OracleConnection(_adminConnectionString);
        await connection.OpenAsync();
        await ExecuteOnAsync(connection, $"DROP USER {_ownedUser} CASCADE");
        _ownedUser = null;
    }

    [TestMethod]
    public async Task Existing_repository_stages_crud_and_round_trips_audit_decimal_raw_guid_and_utc_timestamp()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<ILoggedUserService<int>>().Authenticate(new TestUser { Id = 42 });
        var item = NewRecord("café 日本語");
        await repository.AddAsync(item);
        Assert.AreEqual(0m, await CountAsync());
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
        Assert.IsNull(loaded.Note);
        Assert.IsNull(loaded.ModificationDate);
        loaded.Name = "renamed";
        await repository.UpdateAsync(loaded);
        await unitOfWork.CommitChangesAsync();
        Assert.AreEqual(42, loaded.ModifiedByUserId);
        Assert.IsNotNull(loaded.ModificationDate);
        await repository.DeleteAsync(loaded);
        await unitOfWork.CommitChangesAsync();
        Assert.IsNull(await repository.GetByIdAsync(item.Id));
    }

    [TestMethod]
    public async Task Existing_guid_identifier_repository_uses_explicit_raw16_mapping()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<GuidRepository>();
        var record = new GuidRecord { Id = Guid.NewGuid(), Name = "guid-identity" };
        await repository.AddAsync(record);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitChangesAsync();
        scope.ServiceProvider.GetRequiredService<GuidContext>().ChangeTracker.Clear();
        var loaded = await repository.GetByIdAsync(record.Id);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(record.Id, loaded.Id);
        Assert.AreEqual(record.Name, loaded.Name);
    }

    [TestMethod]
    public async Task Named_binding_ignores_mapper_order_and_parameterless_commands_reuse_connection()
    {
        await using var connection = new OracleConnection(_connectionString);
        await new InsertProcedure().ExecuteAsync(connection, new InsertParameters("named", 12.3456m));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1m, await CountAsync());
        await new NoParameterProcedure().ExecuteAsync(connection);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1m, (await new ScalarProcedure().ExecuteAsync(connection, null))!.Value);
    }

    [TestMethod]
    public async Task Ref_cursors_preserve_one_empty_middle_and_sixteen_tuple_positions()
    {
        await using var connection = new OracleConnection(_connectionString);
        Assert.AreEqual(1m, (await new ScalarProcedure().ExecuteAsync(connection, null))!.Value);
        var middle = await new EmptyMiddleProcedure().ExecuteAsync(connection, null);
        Assert.AreEqual(1m, middle.Item1!.Value);
        Assert.IsNull(middle.Item2);
        Assert.AreEqual(3m, middle.Item3!.Value);
        ITuple tuple = await new SixteenProcedure().ExecuteAsync(connection, null);
        Assert.AreEqual(16, tuple.Length);
        for (var index = 0; index < tuple.Length; index++)
            Assert.AreEqual((decimal)index + 1, ((ScalarRow)tuple[index]!).Value);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(7m, (await new CustomCursorProcedure().ExecuteAsync(connection, null))!.Value);
        await connection.OpenAsync();
        Assert.AreEqual(1m, (await new ScalarProcedure().ExecuteAsync(connection, null))!.Value);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    public async Task Number_results_convert_checked_integrals_and_only_zero_or_one_to_boolean()
    {
        await using var connection = new OracleConnection(_connectionString);
        Assert.AreEqual(1, await new PrimitiveProcedure<int>().ExecuteAsync(connection, null));
        Assert.AreEqual((byte)1, await new PrimitiveProcedure<byte>().ExecuteAsync(connection, null));
        Assert.IsTrue(await new PrimitiveProcedure<bool>().ExecuteAsync(connection, null));
        Assert.IsFalse(await new PrimitiveProcedure<bool>("ZeroResult").ExecuteAsync(connection, null));
        await Assert.ThrowsExactlyAsync<OverflowException>(() => new PrimitiveProcedure<byte>("OverflowResult").ExecuteAsync(connection, null));
        await Assert.ThrowsExactlyAsync<InvalidCastException>(() => new PrimitiveProcedure<int>("FractionalResult").ExecuteAsync(connection, null));
        await Assert.ThrowsExactlyAsync<InvalidCastException>(() => new PrimitiveProcedure<bool>("InvalidBooleanResult").ExecuteAsync(connection, null));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1, await new PrimitiveProcedure<int>().ExecuteAsync(connection, null));
    }

    [TestMethod]
    public async Task Shared_repository_search_maps_metadata_and_ordered_rows_including_an_empty_page()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        await repository.AddAsync(new[] { NewRecord("first"), NewRecord("second"), NewRecord("third") });
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitChangesAsync();
        var result = await repository.SearchAsync(new SearchParameters { PageNumber = 2, PageSize = 2 });
        Assert.AreEqual(3, result.PageInfo.ItemsCount);
        Assert.AreEqual(2, result.PageInfo.TotalPages);
        CollectionAssert.AreEqual(new[] { "third" }, result.Results.Select(item => item.Name).ToArray());
        var empty = await repository.SearchAsync(new SearchParameters { PageNumber = 5, PageSize = 2 });
        Assert.AreEqual(3, empty.PageInfo.ItemsCount);
        Assert.HasCount(0, empty.Results.ToArray());
    }

    [TestMethod]
    public async Task Two_contexts_and_procedure_share_commit_or_rollback_through_existing_unit_of_work()
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
            var connection = first.Database.GetDbConnection();
            Assert.AreSame(connection, second.Database.GetDbConnection());
            using (var transaction = unitOfWork.CreateTransaction())
            {
                await repository.AddAsync(NewRecord("first-" + commit));
                second.Add(NewRecord("second-" + commit));
                await unitOfWork.CommitChangesAsync();
                await new InsertProcedure().ExecuteAsync(connection, new InsertParameters("routine-" + commit), unitOfWork);
                Assert.AreEqual(ConnectionState.Open, connection.State);
                if (commit)
                    transaction.Commit();
                else
                    transaction.Rollback();
            }
            Assert.AreEqual(commit ? 3m : 0m, await CountAsync());
        }
    }

    [TestMethod]
    public async Task Different_connection_is_rejected_before_executing_outside_the_transaction()
    {
        using var services = CreateServices();
        using var scope = services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        using var transaction = unitOfWork.CreateTransaction();
        await using var connection = new OracleConnection(_connectionString);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => new InsertProcedure()
            .ExecuteAsync(connection, new InsertParameters("must-not-write"), unitOfWork));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.IsTrue(transaction.IsActive);
        transaction.Rollback();
        Assert.AreEqual(0m, await CountAsync());
    }

    [TestMethod]
    public async Task Mapping_and_provider_failures_close_open_connections_and_preserve_reuse()
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        var failure = await Assert.ThrowsExactlyAsync<FormatException>(() => new BrokenProcedure().ExecuteAsync(connection, null));
        Assert.AreEqual("synthetic mapping failure", failure.Message);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        await Assert.ThrowsExactlyAsync<OracleException>(() => new NoParameterProcedure("MissingProcedure").ExecuteAsync(connection));
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1m, (await new ScalarProcedure().ExecuteAsync(connection, null))!.Value);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task Native_command_timeout_cancels_slow_procedure_and_allows_reuse()
    {
        await using var connection = new OracleConnection(_connectionString);
        var elapsed = Stopwatch.StartNew();
        var failure = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => new NoParameterProcedure("SlowProcedure", 1).ExecuteAsync(connection));
        Assert.IsInstanceOfType<OracleException>(failure.InnerException);
        Assert.AreEqual(1013, ((OracleException)failure.InnerException).Number);
        Assert.IsTrue(elapsed.Elapsed < TimeSpan.FromSeconds(9), "A one-second driver timeout should cancel the ten-second procedure.");
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.AreEqual(1m, (await new ScalarProcedure().ExecuteAsync(connection, null))!.Value);
        Assert.AreEqual(1m, (await new ScalarProcedure(timeout: int.MaxValue).ExecuteAsync(connection, null))!.Value);
    }

    [TestMethod]
    public async Task Constraint_failure_does_not_commit_pending_transaction_writes()
    {
        await using var connection = new OracleConnection(_connectionString);
        await new InsertProcedure().ExecuteAsync(connection, new InsertParameters("unique"));
        using var services = CreateServices();
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<AuditRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var scopedConnection = scope.ServiceProvider.GetRequiredService<CatalogContext>().Database.GetDbConnection();
        using var transaction = unitOfWork.CreateTransaction();
        await repository.AddAsync(NewRecord("pending"));
        await unitOfWork.CommitChangesAsync();
        var error = await Assert.ThrowsExactlyAsync<OracleException>(() => new InsertProcedure().ExecuteAsync(scopedConnection, new InsertParameters("unique"), unitOfWork));
        Assert.AreEqual(1, error.Number);
        Assert.IsTrue(transaction.IsActive);
        Assert.AreEqual(ConnectionState.Open, scopedConnection.State);
        transaction.Rollback();
        Assert.AreEqual(1m, await CountAsync());
    }

    [TestMethod]
    public async Task Scope_owns_connection_and_context_disposal_keeps_it_usable()
    {
        using var services = CreateServices();
        OracleConnection connection;
        using (var scope = services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
            connection = (OracleConnection)context.Database.GetDbConnection();
            await connection.OpenAsync();
            context.Dispose();
            Assert.AreEqual(ConnectionState.Open, connection.State);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM DUAL";
            Assert.AreEqual(1m, Convert.ToDecimal(await command.ExecuteScalarAsync()));
        }
        Assert.AreEqual(ConnectionState.Closed, connection.State);
        Assert.ThrowsExactly<ObjectDisposedException>(() => connection.Open());
    }

    #endregion

    #region Private Methods

    private ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Integration"] = _connectionString }).Build());
        services.AddScoped<OracleDbConnectionProvider>();
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
        OccurredAt = new DateTime(2026, 11, 14, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234560),
        CreationDate = DateTime.UtcNow
    };

    private static void ConfigureModel(ModelBuilder builder)
    {
        var record = builder.Entity<AuditRecord>();
        record.ToTable("AuditRecord");
        record.Property(item => item.Id).UseIdentityColumn();
        record.Property(item => item.Name).HasColumnType("NVARCHAR2(100)");
        record.Property(item => item.Note).HasColumnType("NVARCHAR2(100)");
        record.Property(item => item.Amount).HasColumnType("NUMBER(18,4)");
        record.Property(item => item.Token).HasColumnType("RAW(16)");
        record.Property(item => item.IsActive).HasColumnType("NUMBER(1)");
        record.Property(item => item.OccurredAt).HasColumnType("TIMESTAMP(6)")
            .HasConversion(value => value.ToUniversalTime(), value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        record.Property(item => item.CreationDate).HasColumnType("TIMESTAMP(6)");
        record.Property(item => item.ModificationDate).HasColumnType("TIMESTAMP(6)");
    }

    private async Task<decimal> CountAsync()
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"AuditRecord\"";
        return Convert.ToDecimal(await command.ExecuteScalarAsync());
    }

    private static async Task ExecuteOnAsync(OracleConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 20;
        await command.ExecuteNonQueryAsync();
    }

    private async Task CreateSchemaAsync()
    {
        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteOnAsync(connection, """
            CREATE TABLE "AuditRecord" (
                "Id" NUMBER(10) GENERATED BY DEFAULT ON NULL AS IDENTITY PRIMARY KEY,
                "Name" NVARCHAR2(100) NOT NULL UNIQUE,
                "Note" NVARCHAR2(100) NULL,
                "Amount" NUMBER(18,4) NOT NULL,
                "Token" RAW(16) NOT NULL,
                "IsActive" NUMBER(1) NOT NULL,
                "OccurredAt" TIMESTAMP(6) NOT NULL,
                "CreatedByUserId" NUMBER(10) NULL,
                "CreationDate" TIMESTAMP(6) NOT NULL,
                "ModifiedByUserId" NUMBER(10) NULL,
                "ModificationDate" TIMESTAMP(6) NULL
            )
            """);
        await ExecuteOnAsync(connection, """
            CREATE PROCEDURE InsertRecord(p_name IN NVARCHAR2, p_amount IN NUMBER) AS
            BEGIN
                INSERT INTO "AuditRecord" ("Name", "Amount", "Token", "IsActive", "OccurredAt", "CreationDate")
                VALUES(p_name, p_amount, HEXTORAW('00000000000000000000000000000001'), 1, SYS_EXTRACT_UTC(SYSTIMESTAMP), SYS_EXTRACT_UTC(SYSTIMESTAMP));
            END;
            """);
        await ExecuteOnAsync(connection, "CREATE TABLE \"GuidRecord\" (\"Id\" RAW(16) PRIMARY KEY, \"Name\" NVARCHAR2(100) NOT NULL)");
        await ExecuteOnAsync(connection, "CREATE PROCEDURE NoParameters AS BEGIN NULL; END;");
        await ExecuteOnAsync(connection, "CREATE PROCEDURE SlowProcedure AS BEGIN DBMS_SESSION.SLEEP(10); END;");
        await ExecuteOnAsync(connection, "CREATE PROCEDURE SingleResult(p_result1 OUT SYS_REFCURSOR) AS BEGIN OPEN p_result1 FOR SELECT 1 AS \"Value\" FROM DUAL; END;");
        foreach (var (name, number) in new[] { ("ZeroResult", "0"), ("OverflowResult", "256"), ("FractionalResult", "1.5"), ("InvalidBooleanResult", "2") })
            await ExecuteOnAsync(connection, $"CREATE PROCEDURE {name}(p_result1 OUT SYS_REFCURSOR) AS BEGIN OPEN p_result1 FOR SELECT {number} AS \"Value\" FROM DUAL; END;");
        await ExecuteOnAsync(connection, "CREATE PROCEDURE CustomCursor(p_rows OUT SYS_REFCURSOR) AS BEGIN OPEN p_rows FOR SELECT 7 AS \"Value\" FROM DUAL; END;");
        await ExecuteOnAsync(connection, """
            CREATE PROCEDURE EmptyMiddle(p_result1 OUT SYS_REFCURSOR, p_result2 OUT SYS_REFCURSOR, p_result3 OUT SYS_REFCURSOR) AS
            BEGIN
                OPEN p_result1 FOR SELECT 1 AS "Value" FROM DUAL;
                OPEN p_result2 FOR SELECT 2 AS "Value" FROM DUAL WHERE 1 = 0;
                OPEN p_result3 FOR SELECT 3 AS "Value" FROM DUAL;
            END;
            """);
        await ExecuteOnAsync(connection, "CREATE PROCEDURE SixteenResults(" +
            string.Join(", ", Enumerable.Range(1, 16).Select(index => $"p_result{index} OUT SYS_REFCURSOR")) +
            ") AS BEGIN " + string.Join(" ", Enumerable.Range(1, 16).Select(index => $"OPEN p_result{index} FOR SELECT {index} AS \"Value\" FROM DUAL;")) + " END;");
        await ExecuteOnAsync(connection, """
            CREATE PROCEDURE SearchRecords(p_page_size IN NUMBER, p_page_number IN NUMBER, p_result1 OUT SYS_REFCURSOR, p_result2 OUT SYS_REFCURSOR) AS
            BEGIN
                OPEN p_result1 FOR SELECT COUNT(*) AS "ItemsCount", CEIL(COUNT(*) / p_page_size) AS "TotalPages", p_page_number AS "PageNumber" FROM "AuditRecord";
                OPEN p_result2 FOR SELECT "Id", "Name", "Amount", "Token", "IsActive", "OccurredAt" FROM "AuditRecord"
                    ORDER BY "Id" OFFSET ((p_page_number - 1) * p_page_size) ROWS FETCH NEXT p_page_size ROWS ONLY;
            END;
            """);
    }

    #endregion
}
