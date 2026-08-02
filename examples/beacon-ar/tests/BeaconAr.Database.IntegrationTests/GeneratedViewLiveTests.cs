using BeaconAr.Data.Receivables.Context;
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

        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ReceivablesDbContext>().UseSqlServer(connection).Options;
        await using var context = new ReceivablesDbContext(services, options);
        await context.Database.UseTransactionAsync(transaction);

        var user = await context.ApplicationUserViews.AsNoTracking().SingleAsync(item => item.Id == userId);
        var product = await context.ProductViews.AsNoTracking().SingleAsync(item => item.Id == productId);
        var customer = await context.CustomerViews.AsNoTracking().SingleAsync(item => item.Id == customerId);
        var address = await context.CustomerAddressViews.AsNoTracking().SingleAsync(item => item.Id == addressId);
        var carrier = await context.CarrierViews.AsNoTracking().SingleAsync(item => item.Id == carrierId);
        var quote = await context.QuoteViews.AsNoTracking().SingleAsync(item => item.Id == quoteId);
        var quoteLine = await context.QuoteLineViews.AsNoTracking().SingleAsync(item => item.Id == quoteLineId);
        var order = await context.SalesOrderViews.AsNoTracking().SingleAsync(item => item.Id == orderId);
        var orderLine = await context.SalesOrderLineViews.AsNoTracking().SingleAsync(item => item.Id == orderLineId);

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

        await transaction.RollbackAsync();
    }

    #endregion

    #region Private Methods

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
