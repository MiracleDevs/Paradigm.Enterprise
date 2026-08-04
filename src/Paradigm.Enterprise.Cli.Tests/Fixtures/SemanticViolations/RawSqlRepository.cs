using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Domain.Repositories;

namespace SemanticViolations;

public sealed partial class RawSqlRepository(
    OrdersContext context,
    DbConnection connection,
    IDbConnection contractConnection) : IRepository
{
    #region Constants

    private const string SelectSql = """
        SELECT Id, Status
        FROM dbo.Orders
        """;

    #endregion

    #region Fields

    private string? _assignedField;
    private string? _harmlessField;

    #endregion

    #region Properties

    private string? AssignedProperty { get; set; }
    private string DeleteSql => "DELETE FROM " + "dbo.Orders WHERE Id = 1";

    #endregion

    #region Constructors

    public RawSqlRepository(
        OrdersContext context,
        DbConnection connection,
        IDbConnection contractConnection,
        bool assignSql) : this(context, connection, contractConnection)
    {
        _assignedField = "SELECT Id FROM dbo.AssignedRows";
        AssignedProperty = "DELETE FROM dbo.AssignedRows";
        _assignedField += " UPDATE dbo.AssignedRows SET Id = 1";
        AssignedProperty ??= BuildSqlLiteral();
        _harmlessField = assignSql ? "Select the reviewed option." : "Select another option.";
        _ = _harmlessField;
    }

    #endregion

    #region Public Methods

    public void Dispose()
    {
    }

    public async Task EfRawSqlAsync(string status)
    {
        await context.Orders.FromSqlRaw("SELECT * FROM dbo.Orders").ToListAsync();
        await context.Orders.FromSqlInterpolated($"SELECT * FROM dbo.Orders WHERE Status = {status}").ToListAsync();
        await context.Database.ExecuteSqlRawAsync("UPDATE dbo.Orders SET Status = 'open'");
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.Orders SET Status = {status}");
        await context.Database.SqlQueryRaw<int>("SELECT Id FROM dbo.Orders").ToListAsync();
        await context.Database.SqlQuery<int>($"SELECT Id FROM dbo.Orders WHERE Status = {status}").ToListAsync();
    }

    public async Task AdoAsync()
    {
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql;
        await command.ExecuteReaderAsync();
        await using var initialized = new SqlCommand
        {
            CommandText = "SELECT Status FROM dbo.Orders"
        };
        await initialized.ExecuteScalarAsync();
        using var contractCommand = contractConnection.CreateCommand();
        contractCommand.CommandText = "SELECT Id FROM dbo.Orders";
        contractCommand.ExecuteReader();
    }

    public async Task ConditionalAdoAsync(DbCommand? optionalCommand)
    {
        _ = connection?.CreateCommand();
        _ = optionalCommand?.ExecuteReader();
        await (connection?.QueryAsync<Order>("SELECT Id FROM dbo.ConditionalOrders") ??
               Task.FromResult<IReadOnlyList<Order>>([]));
        await (connection?.ExecuteStatementAsync("DELETE FROM dbo.ConditionalOrders") ??
               Task.FromResult(0));
        await (connection?.ExecuteWithMisleadingSqlAsync(42) ?? Task.FromResult(0));
    }

    public async Task RuntimeSqlAsync(string runtimeStatement, FormattableString runtimeQuery)
    {
        await connection.ExecuteStatementAsync(runtimeStatement);
        await (contractConnection?.QueryContractInterpolatedAsync<Order>(runtimeQuery) ??
               Task.FromResult<IReadOnlyList<Order>>([]));
    }

    public async Task DapperLikeAsync()
    {
        await connection.QueryAsync<Order>("SELECT * FROM dbo.Orders");
        await connection.ExecuteAsync("DELETE FROM dbo.Orders WHERE Id = 1");
        await connection.ExecuteReaderAsync("SELECT Id FROM dbo.Orders");
        await connection.ExecuteScalarAsync("SELECT COUNT(*) FROM dbo.Orders");
        await connection.QueryRowsAsync<Order>("SELECT * FROM dbo.Orders");
        await contractConnection.QueryContractAsync<Order>("SELECT * FROM dbo.Orders");
        await DapperLikeExtensions.ExecuteAsync(connection, "UPDATE dbo.Orders SET Status = 'static'");
    }

    public string HelperSql() => SelectSql;

    public string LocalHelperSql()
    {
        const string updateSql = "UPDATE dbo.Orders SET Status = 'closed'";
        string Query() => "WITH rows AS (SELECT Id FROM dbo.Orders) SELECT Id FROM rows";
        return updateSql + Query();
    }

    #endregion

    #region Private Methods

    private string BuildSqlLiteral() => "INSERT INTO dbo.AssignedRows (Id) VALUES (1)";

    #endregion
}
