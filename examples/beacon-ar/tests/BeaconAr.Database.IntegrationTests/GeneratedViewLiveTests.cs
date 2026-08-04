using BeaconAr.Data.Access.Repositories;
using BeaconAr.Data.MasterData.Repositories;
using BeaconAr.Data.Operations.Repositories;
using BeaconAr.Data.Sales.Repositories;
using BeaconAr.Data.Access.Context;
using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.Operations.Context;
using BeaconAr.Data.Sales.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BeaconAr.Database.IntegrationTests;

[TestClass]
[TestCategory("Integration")]
public sealed class GeneratedViewLiveTests
{
    #region Public Methods

    [TestMethod]
    public async Task AllPublicEntityViewsAreQueryableAndExpandCommonReferences()
    {
        string connectionString = GetConnectionString();
        string key = Guid.NewGuid().ToString("N")[..12];
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        (int userId, int productId, int customerId, int addressId, int carrierId, int quoteId, int quoteLineId,
            int orderId, int orderLineId) = await InsertFixtureAsync(connection, transaction, key);
        byte[] keyHash = Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray();
        byte[] requestHash = Enumerable.Range(33, 32).Select(static value => (byte)value).ToArray();
        (long auditLogId, long idempotencyRequestId) = await InsertOperationsFixtureAsync(
            connection, transaction, key, userId, keyHash, requestHash);

        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using var access = new AccessDbContext(services,
            new DbContextOptionsBuilder<AccessDbContext>().UseSqlServer(connection).Options);
        await using var masterData = new MasterDataDbContext(services,
            new DbContextOptionsBuilder<MasterDataDbContext>().UseSqlServer(connection).Options);
        await using var operations = new OperationsDbContext(services,
            new DbContextOptionsBuilder<OperationsDbContext>().UseSqlServer(connection).Options);
        await using var sales = new SalesDbContext(services,
            new DbContextOptionsBuilder<SalesDbContext>().UseSqlServer(connection).Options);
        await access.Database.UseTransactionAsync(transaction);
        await masterData.Database.UseTransactionAsync(transaction);
        await operations.Database.UseTransactionAsync(transaction);
        await sales.Database.UseTransactionAsync(transaction);

        var user = await access.ApplicationUserViews.AsNoTracking().SingleAsync(item => item.Id == userId);
        var product = await masterData.ProductViews.AsNoTracking().SingleAsync(item => item.Id == productId);
        var customer = await masterData.CustomerViews.AsNoTracking().SingleAsync(item => item.Id == customerId);
        var address = await masterData.CustomerAddressViews.AsNoTracking().SingleAsync(item => item.Id == addressId);
        var carrier = await masterData.CarrierViews.AsNoTracking().SingleAsync(item => item.Id == carrierId);
        var quote = await sales.QuoteViews.AsNoTracking().SingleAsync(item => item.Id == quoteId);
        var quoteLine = await sales.QuoteLineViews.AsNoTracking().SingleAsync(item => item.Id == quoteLineId);
        var order = await sales.SalesOrderViews.AsNoTracking().SingleAsync(item => item.Id == orderId);
        var orderLine = await sales.SalesOrderLineViews.AsNoTracking().SingleAsync(item => item.Id == orderLineId);
        var auditLog = await operations.AuditLogViews.AsNoTracking().SingleAsync(item => item.Id == auditLogId);
        var idempotencyRequest = await operations.IdempotencyRequestViews.AsNoTracking()
            .SingleAsync(item => item.Id == idempotencyRequestId);
        var idempotencyState = await operations.IdempotencyStateViews.AsNoTracking().SingleAsync(item => item.Id == 1);

        Assert.AreEqual($"EFPT User {key}", user.DisplayName);
        Assert.AreEqual(user.DisplayName, product.CreatedByUserDisplayName);
        Assert.AreEqual(user.DisplayName, customer.CreatedByUserDisplayName);
        Assert.AreEqual(customer.Name, address.CustomerName);
        Assert.AreEqual("both", address.AddressTypeCode);
        Assert.AreEqual(user.DisplayName, carrier.CreatedByUserDisplayName);
        Assert.AreEqual(customer.Name, quote.CustomerName);
        Assert.AreEqual("draft", quote.StatusCode);
        Assert.AreEqual(product.Name, quoteLine.ProductName);
        Assert.AreEqual(customer.Name, order.CustomerName);
        Assert.AreEqual(carrier.Name, order.CarrierName);
        Assert.AreEqual(product.Name, orderLine.ProductName);
        Assert.AreEqual(user.DisplayName, auditLog.UserDisplayName);
        Assert.AreEqual("{\"fixture\":true}", auditLog.MetadataJson);
        Assert.AreEqual(user.DisplayName, idempotencyRequest.UserDisplayName);
        Assert.AreEqual("in_progress", idempotencyRequest.StateCode);
        Assert.AreEqual(idempotencyState.DisplayName, idempotencyRequest.StateDisplayName);
        Assert.IsNull(idempotencyRequest.CreatedByUserDisplayName);
        Assert.IsNull(idempotencyRequest.ModifiedByUserDisplayName);
        CollectionAssert.AreEqual(keyHash, idempotencyRequest.KeyHash);
        CollectionAssert.AreEqual(requestHash, idempotencyRequest.RequestHash);
        Assert.AreEqual(1, await operations.AuditLogs.AsNoTracking().CountAsync(item => item.Id == auditLogId));
        Assert.AreEqual(1, await operations.AuditLogViews.AsNoTracking().CountAsync(item => item.Id == auditLogId));
        Assert.AreEqual(1, await operations.IdempotencyRequests.AsNoTracking().CountAsync(item => item.Id == idempotencyRequestId));
        Assert.AreEqual(1, await operations.IdempotencyRequestViews.AsNoTracking().CountAsync(item => item.Id == idempotencyRequestId));

        await transaction.RollbackAsync();
    }

    #endregion

    #region Private Methods

    private static async Task<(long AuditLogId, long IdempotencyRequestId)> InsertOperationsFixtureAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key,
        int userId,
        byte[] keyHash,
        byte[] requestHash)
    {
        const string sql = """
            DECLARE @AuditLogId BIGINT;
            DECLARE @IdempotencyRequestId BIGINT;

            INSERT INTO [dbo].[AuditLog]
                ([ResourceType], [ResourceId], [Action], [UserId], [RecordedAt], [CorrelationId], [MetadataJson])
            VALUES
                (N'GeneratedViewFixture', @Key, N'Created', @UserId, SYSDATETIMEOFFSET(), N'efpt-' + @Key, N'{"fixture":true}');
            SET @AuditLogId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[IdempotencyRequest]
                ([UserId], [Operation], [KeyHash], [RequestHash], [StateId], [CreationDate], [ExpirationDate])
            VALUES
                (@UserId, N'generated-view-' + @Key, @KeyHash, @RequestHash, 1, SYSDATETIMEOFFSET(), DATEADD(MINUTE, 5, SYSDATETIMEOFFSET()));
            SET @IdempotencyRequestId = SCOPE_IDENTITY();

            SELECT @AuditLogId, @IdempotencyRequestId;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Key", key);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.Add("@KeyHash", System.Data.SqlDbType.Binary, 32).Value = keyHash;
        command.Parameters.Add("@RequestHash", System.Data.SqlDbType.Binary, 32).Value = requestHash;
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    private static async Task<(int UserId, int ProductId, int CustomerId, int AddressId, int CarrierId,
        int QuoteId, int QuoteLineId, int OrderId, int OrderLineId)> InsertFixtureAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string key)
    {
        const string sql = """
            DECLARE @UserId INT;
            DECLARE @ProductId INT;
            DECLARE @CustomerId INT;
            DECLARE @AddressId INT;
            DECLARE @CarrierId INT;
            DECLARE @QuoteId INT;
            DECLARE @QuoteLineId INT;
            DECLARE @OrderId INT;
            DECLARE @OrderLineId INT;

            INSERT INTO [dbo].[ApplicationUser]
                ([Issuer], [Subject], [DisplayName], [Email], [CreationDate])
            VALUES
                (N'https://efpt.tests', @Key, N'EFPT User ' + @Key, N'efpt@example.test', SYSDATETIMEOFFSET());
            SET @UserId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[Product]
                ([Sku], [Name], [Category], [UnitPrice], [StockQuantity], [CreatedByUserId], [CreationDate])
            VALUES
                (N'EFPT-' + @Key, N'EFPT Product ' + @Key, N'Test', 12.50, 4, @UserId, SYSDATETIMEOFFSET());
            SET @ProductId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[Customer]
                ([AccountNumber], [Name], [Email], [CreditLimit], [PaymentTermsDays], [CreatedByUserId], [CreationDate])
            VALUES
                (N'EFPT-' + @Key, N'EFPT Customer ' + @Key, N'customer@example.test', 1000, 30, @UserId, SYSDATETIMEOFFSET());
            SET @CustomerId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[CustomerAddress]
                ([CustomerId], [AddressTypeId], [Label], [Line1], [City], [PostalCode], [Country], [CreatedByUserId], [CreationDate])
            VALUES
                (@CustomerId, 3, N'EFPT Address', N'1 Test Street', N'Buenos Aires', N'1000', N'AR', @UserId, SYSDATETIMEOFFSET());
            SET @AddressId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[Carrier]
                ([Code], [Name], [ServiceLevel], [CreatedByUserId], [CreationDate])
            VALUES
                (N'EFPT-' + @Key, N'EFPT Carrier ' + @Key, N'Ground', @UserId, SYSDATETIMEOFFSET());
            SET @CarrierId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[Quote]
                ([QuoteNumber], [CustomerId], [ShippingAddressId], [QuoteDate], [ValidUntil], [StatusId],
                 [CustomerAccountNumberSnapshot], [CustomerNameSnapshot], [CustomerEmailSnapshot],
                 [ShippingLabelSnapshot], [ShippingLine1Snapshot], [ShippingCitySnapshot],
                 [ShippingPostalCodeSnapshot], [ShippingCountrySnapshot], [ShippingAddressTypeCodeSnapshot],
                 [CreatedByUserId], [CreationDate])
            VALUES
                (N'Q-EFPT-' + @Key, @CustomerId, @AddressId, '2026-08-01', '2026-08-31', 1,
                 N'EFPT-' + @Key, N'EFPT Customer ' + @Key, N'customer@example.test',
                 N'EFPT Address', N'1 Test Street', N'Buenos Aires', N'1000', N'AR', N'both',
                 @UserId, SYSDATETIMEOFFSET());
            SET @QuoteId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[QuoteLine]
                ([QuoteId], [ProductId], [SkuSnapshot], [ProductNameSnapshot], [Quantity], [UnitPrice], [DiscountPercent])
            VALUES
                (@QuoteId, @ProductId, N'EFPT-' + @Key, N'EFPT Product ' + @Key, 2, 12.50, 0);
            SET @QuoteLineId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[SalesOrder]
                ([OrderNumber], [CustomerId], [ShippingAddressId], [StatusId], [CarrierId], [CreatedByUserId], [CreationDate])
            VALUES
                (N'O-EFPT-' + @Key, @CustomerId, @AddressId, 1, @CarrierId, @UserId, SYSDATETIMEOFFSET());
            SET @OrderId = SCOPE_IDENTITY();

            INSERT INTO [dbo].[SalesOrderLine]
                ([SalesOrderId], [ProductId], [SkuSnapshot], [ProductNameSnapshot], [Quantity], [UnitPrice], [DiscountPercent])
            VALUES
                (@OrderId, @ProductId, N'EFPT-' + @Key, N'EFPT Product ' + @Key, 1, 12.50, 0);
            SET @OrderLineId = SCOPE_IDENTITY();

            SELECT @UserId, @ProductId, @CustomerId, @AddressId, @CarrierId,
                   @QuoteId, @QuoteLineId, @OrderId, @OrderLineId;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@Key", key);
        await using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.IsTrue(await reader.ReadAsync());
        return (reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4),
            reader.GetInt32(5), reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8));
    }

    private static string GetConnectionString()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set ConnectionStrings__DatabaseConnection to run database integration tests.");
        return connectionString;
    }

    #endregion
}
