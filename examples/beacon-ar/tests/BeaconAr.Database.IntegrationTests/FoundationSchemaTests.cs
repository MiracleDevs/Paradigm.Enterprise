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
