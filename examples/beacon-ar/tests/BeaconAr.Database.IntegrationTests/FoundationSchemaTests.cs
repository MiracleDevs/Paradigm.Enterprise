using BeaconAr.Data.Access.Repositories;
using BeaconAr.Data.MasterData.Repositories;
using BeaconAr.Data.Operations.Repositories;
using BeaconAr.Data.Sales.Repositories;
using Microsoft.Data.SqlClient;

namespace BeaconAr.Database.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public sealed class FoundationSchemaTests
{
    #region Public Methods

    [TestMethod]
    public async Task FoundationSmokeScriptPasses()
    {
        await using var connection = await OpenConnectionAsync();
        var script = await File.ReadAllTextAsync(Path.Combine(FindExampleRoot(), "src", "database", "scripts", "verification", "FoundationSmoke.sql"));
        await using var command = new SqlCommand(script, connection);

        await command.ExecuteNonQueryAsync();
    }

    [TestMethod]
    public async Task OidcIdentityComparisonIsExact()
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        const string sql = """
            INSERT INTO [dbo].[ApplicationUser] ([Issuer], [Subject], [DisplayName], [CreationDate])
            VALUES (N'https://issuer.example/Tenant', N'Subject', N'First', SYSDATETIMEOFFSET());
            INSERT INTO [dbo].[ApplicationUser] ([Issuer], [Subject], [DisplayName], [CreationDate])
            VALUES (N'https://issuer.example/tenant', N'subject', N'Second', SYSDATETIMEOFFSET());
            """;
        await using var command = new SqlCommand(sql, connection, transaction);

        Assert.AreEqual(2, await command.ExecuteNonQueryAsync());
        await transaction.RollbackAsync();
    }

    [TestMethod]
    public async Task ContextBoundaryRoutineContractsAreExact()
    {
        await using var connection = await OpenConnectionAsync();
        var contracts = new Dictionary<string, (string[] Parameters, string[] Results)>(StringComparer.Ordinal)
        {
            ["dbo.GetOrCreateApplicationUser"] =
                (["@Issuer:nvarchar(400)", "@Subject:nvarchar(200)", "@DisplayName:nvarchar(200)", "@Email:nvarchar(320)", "@Now:datetimeoffset(7)"], ["<unnamed>:int"]),
            ["dbo.LockCustomerForAddressMutation"] = (["@Id:int"], ["<unnamed>:int"]),
            ["dbo.HasAddressReferences"] = (["@Id:int"], ["<unnamed>:bit"]),
            ["dbo.HasCarrierReferences"] = (["@Id:int"], ["<unnamed>:bit"]),
            ["dbo.HasCustomerReferences"] = (["@Id:int"], ["<unnamed>:bit"]),
            ["dbo.HasProductReferences"] = (["@Id:int"], ["<unnamed>:bit"]),
            ["dbo.LockIdempotencyRequest"] =
                (["@UserId:int", "@Operation:nvarchar(150)", "@KeyHash:binary(32)"], ["<unnamed>:bigint"]),
            ["dbo.GetDashboardSummary"] =
                ([], ["Products:bigint", "Customers:bigint", "Carriers:bigint", "OpenQuotes:bigint", "ActiveOrders:bigint", "AsOf:datetimeoffset(7)"]),
            ["dbo.AllocateQuoteNumber"] = ([], ["<unnamed>:bigint"]),
            ["dbo.AllocateSalesOrderNumber"] = ([], ["<unnamed>:bigint"]),
            ["dbo.LockQuoteForConversion"] = (["@Id:int"], ["<unnamed>:int"]),
            ["dbo.GetCustomerSalesReferenceForUpdate"] =
                (["@Id:int"], ["Id:int", "AccountNumber:nvarchar(50)", "Name:nvarchar(120)", "Email:nvarchar(320)", "Phone:nvarchar(50)", "IsActive:bit"]),
            ["dbo.GetAddressSalesReferenceForUpdate"] =
                (["@Id:int"], ["Id:int", "CustomerId:int", "AddressTypeCode:nvarchar(32)", "Label:nvarchar(120)", "Line1:nvarchar(200)", "Line2:nvarchar(200)", "City:nvarchar(120)", "State:nvarchar(120)", "PostalCode:nvarchar(32)", "Country:nchar(2)"]),
            ["dbo.GetProductSalesReferenceForUpdate"] =
                (["@Id:int"], ["Id:int", "Sku:nvarchar(32)", "Name:nvarchar(120)", "IsActive:bit"]),
            ["dbo.GetCarrierSalesReferenceForUpdate"] =
                (["@Id:int"], ["Id:int", "Name:nvarchar(120)", "IsActive:bit"]),
        };

        foreach ((string routine, (string[] parameters, string[] results)) in contracts)
            await AssertRoutineContractAsync(connection, routine, parameters, results);
    }

    [TestMethod]
    public async Task TransactionRootsRejectAnotherCustomersShippingAddress()
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var (firstCustomerId, otherAddressId) = await CreateCustomersAndOtherAddressAsync(connection, transaction);

        var quoteError = await Assert.ThrowsAsync<SqlException>(() => InsertMismatchedQuoteAsync(connection, transaction, firstCustomerId, otherAddressId));
        Assert.AreEqual(547, quoteError.Number);
        StringAssert.Contains(quoteError.Message, "FK_Quote_CustomerAddress");
        var orderError = await Assert.ThrowsAsync<SqlException>(() => InsertMismatchedOrderAsync(connection, transaction, firstCustomerId, otherAddressId));
        Assert.AreEqual(547, orderError.Number);
        StringAssert.Contains(orderError.Message, "FK_SalesOrder_CustomerAddress");
        await transaction.RollbackAsync();
    }

    #endregion

    #region Private Methods

    private static async Task AssertRoutineContractAsync(
        SqlConnection connection,
        string routine,
        string[] expectedParameters,
        string[] expectedResults)
    {
        const string parameterSql = """
            SELECT p.[name],
                CASE
                    WHEN TYPE_NAME(p.[user_type_id]) IN (N'nchar', N'nvarchar') THEN
                        CONCAT(TYPE_NAME(p.[user_type_id]), N'(', IIF(p.[max_length] = -1, N'max', CONVERT(NVARCHAR(10), p.[max_length] / 2)), N')')
                    WHEN TYPE_NAME(p.[user_type_id]) IN (N'binary', N'char', N'varbinary', N'varchar') THEN
                        CONCAT(TYPE_NAME(p.[user_type_id]), N'(', IIF(p.[max_length] = -1, N'max', CONVERT(NVARCHAR(10), p.[max_length])), N')')
                    WHEN TYPE_NAME(p.[user_type_id]) IN (N'decimal', N'numeric') THEN
                        CONCAT(TYPE_NAME(p.[user_type_id]), N'(', p.[precision], N',', p.[scale], N')')
                    WHEN TYPE_NAME(p.[user_type_id]) IN (N'datetime2', N'datetimeoffset', N'time') THEN
                        CONCAT(TYPE_NAME(p.[user_type_id]), N'(', p.[scale], N')')
                    ELSE TYPE_NAME(p.[user_type_id])
                END AS [TypeName]
            FROM sys.parameters AS p
            WHERE p.[object_id] = OBJECT_ID(@Routine)
            ORDER BY p.[parameter_id];
            """;
        await using var parameterCommand = new SqlCommand(parameterSql, connection);
        parameterCommand.Parameters.AddWithValue("@Routine", routine);
        var actualParameters = new List<string>();
        await using (SqlDataReader reader = await parameterCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                actualParameters.Add($"{reader.GetString(0)}:{reader.GetString(1)}");
        }
        CollectionAssert.AreEqual(expectedParameters, actualParameters, $"Parameter contract changed for {routine}.");

        const string resultSql = """
            SELECT COALESCE([name], N'<unnamed>'), [system_type_name]
            FROM sys.dm_exec_describe_first_result_set_for_object(OBJECT_ID(@Routine), 0)
            WHERE [is_hidden] = 0
            ORDER BY [column_ordinal];
            """;
        await using var resultCommand = new SqlCommand(resultSql, connection);
        resultCommand.Parameters.AddWithValue("@Routine", routine);
        var actualResults = new List<string>();
        await using (SqlDataReader reader = await resultCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                actualResults.Add($"{reader.GetString(0)}:{reader.GetString(1)}");
        }
        CollectionAssert.AreEqual(expectedResults, actualResults, $"Result contract changed for {routine}.");
    }

    private static async Task<(int FirstCustomerId, int OtherAddressId)> CreateCustomersAndOtherAddressAsync(
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
            INSERT INTO [dbo].[Customer] ([AccountNumber], [Name], [Email], [CreditLimit], [PaymentTermsDays], [CreationDate])
            VALUES (CONCAT(N'FIRST-', NEWID()), N'First', N'first@example.test', 0, 0, SYSDATETIMEOFFSET());
            DECLARE @FirstCustomerId INT = CONVERT(INT, SCOPE_IDENTITY());
            INSERT INTO [dbo].[Customer] ([AccountNumber], [Name], [Email], [CreditLimit], [PaymentTermsDays], [CreationDate])
            VALUES (CONCAT(N'OTHER-', NEWID()), N'Other', N'other@example.test', 0, 0, SYSDATETIMEOFFSET());
            DECLARE @OtherCustomerId INT = CONVERT(INT, SCOPE_IDENTITY());
            INSERT INTO [dbo].[CustomerAddress] ([CustomerId], [AddressTypeId], [Label], [Line1], [City], [PostalCode], [Country], [CreationDate])
            VALUES (@OtherCustomerId, 2, N'Other shipping', N'1 Other Street', N'Other City', N'1000', N'AR', SYSDATETIMEOFFSET());
            SELECT @FirstCustomerId, CONVERT(INT, SCOPE_IDENTITY());
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    private static async Task InsertMismatchedQuoteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int customerId,
        int addressId)
    {
        const string sql = """
            INSERT INTO [dbo].[Quote]
                ([QuoteNumber], [CustomerId], [ShippingAddressId], [QuoteDate], [ValidUntil], [StatusId],
                 [CustomerAccountNumberSnapshot], [CustomerNameSnapshot], [CustomerEmailSnapshot],
                 [ShippingLabelSnapshot], [ShippingLine1Snapshot], [ShippingCitySnapshot],
                 [ShippingPostalCodeSnapshot], [ShippingCountrySnapshot], [ShippingAddressTypeCodeSnapshot], [CreationDate])
            VALUES
                (CONCAT(N'Q-', LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18)), @CustomerId, @AddressId, CAST(GETDATE() AS DATE), CAST(GETDATE() AS DATE), 1,
                 N'FIRST', N'First', N'first@example.test', N'Other shipping', N'1 Other Street', N'Other City',
                 N'1000', N'AR', N'SHIPPING', SYSDATETIMEOFFSET());
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@AddressId", addressId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertMismatchedOrderAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int customerId,
        int addressId)
    {
        const string sql = """
            INSERT INTO [dbo].[SalesOrder] ([OrderNumber], [CustomerId], [ShippingAddressId], [StatusId], [CreationDate])
            VALUES (CONCAT(N'O-', LEFT(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''), 18)), @CustomerId, @AddressId, 1, SYSDATETIMEOFFSET());
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@CustomerId", customerId);
        command.Parameters.AddWithValue("@AddressId", addressId);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<SqlConnection> OpenConnectionAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set ConnectionStrings__DatabaseConnection to run database integration tests.");

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static string FindExampleRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "aspire.config.json")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Beacon AR example root not found.");
    }

    #endregion
}
