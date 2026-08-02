using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Sales.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace BeaconAr.WebApi.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class BeaconArApiAcceptanceTests
{
    #region Constants

    private const string Audience = "api://beacon-live-tests";
    private const string Issuer = "https://issuer.beacon.live.test";
    private const string SigningKey = "beacon-ar-live-contract-tests-signing-key-2026";

    #endregion

    #region Fields

    private static readonly JsonSerializerOptions Json = CreateJsonOptions();

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task Scenario01ProfileDashboardAndMasterSearches()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        foreach (string path in new[] { "/api/v1/me", "/api/v1/dashboard/summary", "/api/v1/products?pageNumber=1&pageSize=10", "/api/v1/customers?pageNumber=1&pageSize=10", "/api/v1/addresses?pageNumber=1&pageSize=10", "/api/v1/carriers?pageNumber=1&pageSize=10" })
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode, path);
    }

    [TestMethod]
    public async Task Scenario02ChangingDefaultAddressKeepsOneDefault()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        CustomerDto customer = await CreateCustomer(client);
        _ = await CreateAddress(client, customer.Id, "First", true);
        _ = await CreateAddress(client, customer.Id, "Second", true);

        PageResult<AddressDto> page = await Read<PageResult<AddressDto>>(await client.GetAsync($"/api/v1/addresses?customerId={customer.Id}&pageSize=100"));

        Assert.AreEqual(1, page.Items.Count(static address => address.DefaultShipping));
        Assert.AreEqual("Second", page.Items.Single(static address => address.DefaultShipping).Label);
    }

    [TestMethod]
    public async Task Scenario03DuplicateNaturalKeysAreConsistentConflicts()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        string suffix = Suffix();
        object product = new { sku = $"SKU-{suffix}", name = "Product", category = "Test", unitPrice = 10.25m, stockQuantity = 1, isActive = true };
        object customer = new { accountNumber = $"ACC-{suffix}", name = "Customer", email = $"{suffix}@example.test", creditLimit = 0m, paymentTermsDays = 30, isActive = true };
        object carrier = new { code = $"CAR-{suffix}", name = "Carrier", serviceLevel = "Ground", isActive = true };
        Assert.AreEqual(HttpStatusCode.Created, (await Post(client, "/api/v1/products", product)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await Post(client, "/api/v1/products", product)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, (await Post(client, "/api/v1/customers", customer)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await Post(client, "/api/v1/customers", customer)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, (await Post(client, "/api/v1/carriers", carrier)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Conflict, (await Post(client, "/api/v1/carriers", carrier)).StatusCode);
    }

    [TestMethod]
    public async Task Scenario04QuoteEditAndLifecycleRejectsSkippedTransition()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        (ProductDto product, CustomerDto customer, AddressDto address, _) = await CreateMasters(client);
        (QuoteDto quote, string etag) = await CreateQuote(client, product, customer, address);
        HttpResponseMessage skipped = await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = "accepted" });
        Assert.AreEqual(HttpStatusCode.Conflict, skipped.StatusCode);
        HttpResponseMessage updated = await Put(client, $"/api/v1/quotes/{quote.Id}", new { customerId = customer.Id, shippingAddressId = address.Id, quoteDate = "2026-08-01", validUntil = "2026-08-31", notes = "edited", lines = new[] { new { productId = product.Id, quantity = 2, unitPrice = 10.005m, discountPercent = 5m } } }, etag);
        Assert.AreEqual(HttpStatusCode.OK, updated.StatusCode);
        HttpResponseMessage sent = await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", ETag(updated), new { status = "sent" });
        Assert.AreEqual(HttpStatusCode.OK, sent.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", ETag(sent), new { status = "accepted" })).StatusCode);
    }

    [TestMethod]
    public async Task Scenario05ConcurrentQuoteConversionReturnsOneOrder()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient first = Client(factory);
        using HttpClient second = Client(factory);
        QuoteDto quote = await CreateAcceptedQuote(first);
        Task<HttpResponseMessage> one = first.PutAsync($"/api/v1/quotes/{quote.Id}/sales-order", null);
        Task<HttpResponseMessage> two = second.PutAsync($"/api/v1/quotes/{quote.Id}/sales-order", null);
        HttpResponseMessage[] responses = await Task.WhenAll(one, two);
        CollectionAssert.AreEquivalent(new[] { HttpStatusCode.Created, HttpStatusCode.OK }, responses.Select(static response => response.StatusCode).ToArray());
        SalesOrderDto[] orders = await Task.WhenAll(responses.Select(Read<SalesOrderDto>));
        Assert.AreEqual(orders[0].Id, orders[1].Id);
    }

    [TestMethod]
    public async Task Scenario06ConvertedOrderRetainsSnapshots()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        QuoteDto quote = await CreateAcceptedQuote(client);
        SalesOrderDto order = await Read<SalesOrderDto>(await client.PutAsync($"/api/v1/quotes/{quote.Id}/sales-order", null));
        ProductDto product = await GetWithETag<ProductDto>(client, $"/api/v1/products/{order.Lines[0].ProductId}");
        HttpResponseMessage changed = await Put(client, $"/api/v1/products/{product.Id}", new { sku = product.Sku, name = "Changed product", category = product.Category, unitPrice = product.UnitPrice, stockQuantity = product.StockQuantity, thumbnailUrl = product.ThumbnailUrl, isActive = false }, $"\"{product.Version}\"");
        Assert.AreEqual(HttpStatusCode.OK, changed.StatusCode);
        SalesOrderDto after = await Read<SalesOrderDto>(await client.GetAsync($"/api/v1/sales-orders/{order.Id}"));
        Assert.AreEqual(order.Lines[0].ProductName, after.Lines[0].ProductName);
        Assert.AreEqual(order.CustomerName, after.CustomerName);
    }

    [TestMethod]
    public async Task Scenario07OrderFulfillmentRequiresCarrierAndTracking()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        QuoteDto quote = await CreateAcceptedQuote(client);
        SalesOrderDto order = await Read<SalesOrderDto>(await client.PutAsync($"/api/v1/quotes/{quote.Id}/sales-order", null));
        CarrierDto carrier = await CreateCarrier(client);
        HttpResponseMessage confirmed = await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", $"\"{order.Version}\"", new { status = "confirmed" });
        HttpResponseMessage processing = await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", ETag(confirmed), new { status = "processing" });
        Assert.AreEqual(HttpStatusCode.Conflict, (await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", ETag(processing), new { status = "shipped" })).StatusCode);
        HttpResponseMessage shipped = await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", ETag(processing), new { status = "shipped", carrierId = carrier.Id, trackingNumber = "TRACK-1" });
        Assert.AreEqual(HttpStatusCode.OK, shipped.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", ETag(shipped), new { status = "completed" })).StatusCode);
    }

    [TestMethod]
    public async Task Scenario08DraftDeletionAndReferenceProtection()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        (ProductDto product, CustomerDto customer, AddressDto address, _) = await CreateMasters(client);
        (QuoteDto quote, string etag) = await CreateQuote(client, product, customer, address);
        using HttpRequestMessage deleteQuote = new(HttpMethod.Delete, $"/api/v1/quotes/{quote.Id}");
        deleteQuote.Headers.TryAddWithoutValidation("If-Match", etag);
        Assert.AreEqual(HttpStatusCode.NoContent, (await client.SendAsync(deleteQuote)).StatusCode);
        (QuoteDto referenced, _) = await CreateQuote(client, product, customer, address);
        using HttpRequestMessage deleteProduct = new(HttpMethod.Delete, $"/api/v1/products/{product.Id}");
        deleteProduct.Headers.TryAddWithoutValidation("If-Match", $"\"{product.Version}\"");
        Assert.AreEqual(HttpStatusCode.Conflict, (await client.SendAsync(deleteProduct)).StatusCode);
        Assert.IsTrue(referenced.Id > 0);
    }

    [TestMethod]
    public async Task Scenario09StaleWriterGetsPreconditionFailed()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient first = Client(factory);
        using HttpClient second = Client(factory);
        ProductDto product = await CreateProduct(first);
        object replacement = new { sku = product.Sku, name = "Winner", category = product.Category, unitPrice = product.UnitPrice, stockQuantity = product.StockQuantity, thumbnailUrl = product.ThumbnailUrl, isActive = true };
        Assert.AreEqual(HttpStatusCode.OK, (await Put(first, $"/api/v1/products/{product.Id}", replacement, $"\"{product.Version}\"")).StatusCode);
        Assert.AreEqual(HttpStatusCode.PreconditionFailed, (await Put(second, $"/api/v1/products/{product.Id}", replacement, $"\"{product.Version}\"")).StatusCode);
    }

    [TestMethod]
    public async Task Scenario10ReadOnlyManagerReadsButCannotWrite()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory, "business.read", "manager-subject");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/v1/dashboard/summary")).StatusCode);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await Post(client, "/api/v1/products", new { sku = "NO-WRITE", name = "No write", category = "Test", unitPrice = 1m, stockQuantity = 0 })).StatusCode);
    }

    [TestMethod]
    public async Task DurableCreationIdempotencyReplaysAndRejectsChangedPayload()
    {
        await using LiveApiFactory firstFactory = CreateFactory();
        string key = $"product-{Suffix()}";
        object body = new { sku = $"SKU-{Suffix()}", name = "Idempotent", category = "Test", unitPrice = 2m, stockQuantity = 1, isActive = true };
        using HttpClient first = Client(firstFactory);
        HttpResponseMessage created = await PostWithIdempotency(first, "/api/v1/products", body, key);
        ProductDto original = await Read<ProductDto>(created);

        await using LiveApiFactory secondFactory = CreateFactory();
        using HttpClient second = Client(secondFactory);
        HttpResponseMessage replay = await PostWithIdempotency(second, "/api/v1/products", body, key);
        ProductDto repeated = await Read<ProductDto>(replay);

        Assert.AreEqual(HttpStatusCode.Created, replay.StatusCode);
        Assert.AreEqual("true", replay.Headers.GetValues("Idempotency-Replayed").Single());
        Assert.AreEqual(original.Id, repeated.Id);
        Assert.AreEqual(HttpStatusCode.Conflict, (await PostWithIdempotency(second, "/api/v1/products", new { sku = body.ToString(), name = "Changed", category = "Test", unitPrice = 3m, stockQuantity = 1 }, key)).StatusCode);
    }

    [TestMethod]
    public async Task ConcurrentFirstUseConvergesAndIssuerRemainsPartOfIdentity()
    {
        string connection = RequiredConnectionString();
        string subject = $"identity-{Suffix()}";
        await using LiveApiFactory firstIssuer = new(connection, Issuer);
        using HttpClient first = Client(firstIssuer, subject: subject);
        using HttpClient second = Client(firstIssuer, subject: subject);

        HttpResponseMessage[] firstUseResponses = await Task.WhenAll(
            first.GetAsync("/api/v1/me"),
            second.GetAsync("/api/v1/me"));
        CurrentUserDto[] sameIssuer = await Task.WhenAll(firstUseResponses.Select(Read<CurrentUserDto>));
        Assert.AreEqual(sameIssuer[0].Id, sameIssuer[1].Id);

        await using LiveApiFactory secondIssuer = new(connection, $"https://second.{Suffix()}.issuer.test");
        using HttpClient other = Client(secondIssuer, subject: subject);
        CurrentUserDto otherIssuer = await Read<CurrentUserDto>(await other.GetAsync("/api/v1/me"));
        Assert.AreNotEqual(sameIssuer[0].Id, otherIssuer.Id);
    }

    [TestMethod]
    public async Task InactiveUserIsRejectedWithoutProfileSynchronization()
    {
        string connection = RequiredConnectionString();
        string subject = $"inactive-{Suffix()}";
        await using LiveApiFactory factory = new(connection);
        using HttpClient active = Client(factory, subject: subject, displayName: "Original Name");
        CurrentUserDto created = await Read<CurrentUserDto>(await active.GetAsync("/api/v1/me"));
        await ExecuteSqlAsync(connection, "UPDATE [dbo].[ApplicationUser] SET [IsActive] = 0 WHERE [Id] = @id", new SqlParameter("@id", created.Id));

        using HttpClient inactive = Client(factory, subject: subject, displayName: "Changed Name");
        HttpResponseMessage denied = await inactive.GetAsync("/api/v1/me");
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        string? displayName = await ScalarAsync<string>(connection, "SELECT [DisplayName] FROM [dbo].[ApplicationUser] WHERE [Id] = @id", new SqlParameter("@id", created.Id));
        Assert.AreEqual("Original Name", displayName);
    }

    [TestMethod]
    public async Task ConcurrentCrossHostCreationIdempotencyReturnsOneResource()
    {
        string connection = RequiredConnectionString();
        await using LiveApiFactory firstFactory = new(connection);
        await using LiveApiFactory secondFactory = new(connection);
        using HttpClient first = Client(firstFactory);
        using HttpClient second = Client(secondFactory);
        string suffix = Suffix();
        string key = $"cross-host-{suffix}";
        object body = new { sku = $"SKU-{suffix}", name = "Cross host", category = "Test", unitPrice = 2m, stockQuantity = 1, isActive = true };

        HttpResponseMessage[] responses = await Task.WhenAll(
            PostWithIdempotency(first, "/api/v1/products", body, key),
            PostWithIdempotency(second, "/api/v1/products", body, key));
        ProductDto[] products = await Task.WhenAll(responses.Select(Read<ProductDto>));

        Assert.IsTrue(responses.All(static response => response.StatusCode == HttpStatusCode.Created));
        Assert.AreEqual(products[0].Id, products[1].Id);
        Assert.AreEqual(1, await ScalarAsync<int>(connection, "SELECT COUNT(*) FROM [dbo].[Product] WHERE [Sku] = @sku", new SqlParameter("@sku", $"SKU-{suffix}")));
    }

    [TestMethod]
    public async Task EveryQuoteLifecycleEdgeAndDeleteRuleIsEnforcedThroughHttp()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        (ProductDto product, CustomerDto customer, AddressDto address, _) = await CreateMasters(client);

        await AssertQuoteTransition(client, product, customer, address, [], "sent", HttpStatusCode.OK);
        await AssertQuoteTransition(client, product, customer, address, ["sent"], "accepted", HttpStatusCode.OK);
        await AssertQuoteTransition(client, product, customer, address, ["sent"], "rejected", HttpStatusCode.OK);
        await AssertQuoteTransition(client, product, customer, address, ["sent"], "expired", HttpStatusCode.OK);

        string[][] sourcePaths = [[], ["sent"], ["sent", "accepted"], ["sent", "rejected"], ["sent", "expired"]];
        string[][] allowedTargets = [["sent"], ["accepted", "rejected", "expired"], [], [], []];
        string[] targets = ["draft", "sent", "accepted", "rejected", "expired"];
        for (int source = 0; source < sourcePaths.Length; source++)
        {
            (QuoteDto quote, string etag) = await CreateQuote(client, product, customer, address);
            foreach (string step in sourcePaths[source])
            {
                HttpResponseMessage advanced = await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = step });
                Assert.AreEqual(HttpStatusCode.OK, advanced.StatusCode);
                etag = ETag(advanced);
            }

            foreach (string target in targets.Except(allowedTargets[source], StringComparer.Ordinal))
                Assert.AreEqual(HttpStatusCode.Conflict,
                    (await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = target })).StatusCode,
                    $"Unexpected quote transition from source index {source} to {target}.");

            using HttpRequestMessage delete = new(HttpMethod.Delete, $"/api/v1/quotes/{quote.Id}");
            delete.Headers.TryAddWithoutValidation("If-Match", etag);
            Assert.AreEqual(source == 0 ? HttpStatusCode.NoContent : HttpStatusCode.Conflict, (await client.SendAsync(delete)).StatusCode);
        }
    }

    [TestMethod]
    public async Task EverySalesOrderLifecycleEdgeAndDeleteRuleIsEnforcedThroughHttp()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        (ProductDto product, CustomerDto customer, AddressDto address, CarrierDto carrier) = await CreateMasters(client);
        string[][] legalPaths =
        [
            ["confirmed"], ["cancelled"], ["confirmed", "processing"], ["confirmed", "cancelled"],
            ["confirmed", "processing", "shipped"], ["confirmed", "processing", "cancelled"],
            ["confirmed", "processing", "shipped", "completed"],
        ];
        foreach (string[] path in legalPaths)
            _ = await CreateAndAdvanceOrder(client, product, customer, address, carrier, path);

        string[][] sourcePaths = [[], ["confirmed"], ["confirmed", "processing"], ["confirmed", "processing", "shipped"], ["confirmed", "processing", "shipped", "completed"], ["cancelled"]];
        string[][] allowedTargets = [["confirmed", "cancelled"], ["processing", "cancelled"], ["shipped", "cancelled"], ["completed"], [], []];
        string[] targets = ["draft", "confirmed", "processing", "shipped", "completed", "cancelled"];
        for (int source = 0; source < sourcePaths.Length; source++)
        {
            (SalesOrderDto order, string etag) = await CreateAndAdvanceOrder(client, product, customer, address, carrier, sourcePaths[source]);
            foreach (string target in targets.Except(allowedTargets[source], StringComparer.Ordinal))
                Assert.AreEqual(HttpStatusCode.Conflict,
                    (await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", etag, TransitionBody(target, carrier.Id))).StatusCode,
                    $"Unexpected order transition from source index {source} to {target}.");

            using HttpRequestMessage delete = new(HttpMethod.Delete, $"/api/v1/sales-orders/{order.Id}");
            delete.Headers.TryAddWithoutValidation("If-Match", etag);
            Assert.AreEqual(source == 0 ? HttpStatusCode.NoContent : HttpStatusCode.Conflict, (await client.SendAsync(delete)).StatusCode);
        }
    }

    [TestMethod]
    public async Task EveryReferencedMasterResourceIsProtectedThroughHttp()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        (ProductDto product, CustomerDto customer, AddressDto address, CarrierDto carrier) = await CreateMasters(client);
        _ = await CreateQuote(client, product, customer, address);
        (SalesOrderDto order, _) = await CreateAndAdvanceOrder(client, product, customer, address, carrier, ["confirmed", "processing", "shipped"]);

        foreach ((string path, int id, string version) in new[]
        {
            ("products", product.Id, product.Version), ("customers", customer.Id, customer.Version),
            ("addresses", address.Id, address.Version), ("carriers", carrier.Id, carrier.Version),
        })
        {
            using HttpRequestMessage delete = new(HttpMethod.Delete, $"/api/v1/{path}/{id}");
            delete.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
            Assert.AreEqual(HttpStatusCode.Conflict, (await client.SendAsync(delete)).StatusCode, path);
        }
        Assert.IsTrue(order.Id > 0);
    }

    [TestMethod]
    public async Task EveryCreationRouteSupportsConcurrentReplayAndChangedPayloadConflict()
    {
        string connection = RequiredConnectionString();
        await using LiveApiFactory firstFactory = new(connection);
        await using LiveApiFactory secondFactory = new(connection);
        using HttpClient first = Client(firstFactory);
        using HttpClient second = Client(secondFactory);
        (ProductDto product, CustomerDto customer, AddressDto address, _) = await CreateMasters(first);
        string suffix = Suffix();
        (string Path, object Body, object Changed)[] cases =
        [
            ("/api/v1/products", new { sku = $"IDP-P-{suffix}", name = "Product", category = "Test", unitPrice = 2m, stockQuantity = 1, isActive = true }, new { sku = $"IDP-P-{suffix}", name = "Changed", category = "Test", unitPrice = 2m, stockQuantity = 1, isActive = true }),
            ("/api/v1/customers", new { accountNumber = $"IDP-C-{suffix}", name = "Customer", email = $"idp-{suffix}@example.test", creditLimit = 0m, paymentTermsDays = 30, isActive = true }, new { accountNumber = $"IDP-C-{suffix}", name = "Changed", email = $"idp-{suffix}@example.test", creditLimit = 0m, paymentTermsDays = 30, isActive = true }),
            ("/api/v1/carriers", new { code = $"IDP-R-{suffix}", name = "Carrier", serviceLevel = "Ground", isActive = true }, new { code = $"IDP-R-{suffix}", name = "Changed", serviceLevel = "Ground", isActive = true }),
            ("/api/v1/addresses", new { customerId = customer.Id, type = "shipping", label = $"IDP-A-{suffix}", line1 = "1 Main", city = "Beacon", postalCode = "1000", country = "US", defaultBilling = false, defaultShipping = false }, new { customerId = customer.Id, type = "shipping", label = "Changed", line1 = "1 Main", city = "Beacon", postalCode = "1000", country = "US", defaultBilling = false, defaultShipping = false }),
            ("/api/v1/quotes", new { customerId = customer.Id, shippingAddressId = address.Id, quoteDate = "2026-08-01", validUntil = "2026-08-31", notes = $"IDP-Q-{suffix}", lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 2m, discountPercent = 0m } } }, new { customerId = customer.Id, shippingAddressId = address.Id, quoteDate = "2026-08-01", validUntil = "2026-08-31", notes = "Changed", lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 2m, discountPercent = 0m } } }),
            ("/api/v1/sales-orders", new { customerId = customer.Id, shippingAddressId = address.Id, requestedShipDate = "2026-08-15", lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 2m, discountPercent = 0m } } }, new { customerId = customer.Id, shippingAddressId = address.Id, requestedShipDate = "2026-08-16", lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 2m, discountPercent = 0m } } }),
        ];

        for (int index = 0; index < cases.Length; index++)
        {
            string key = $"matrix-{index}-{suffix}";
            HttpResponseMessage[] responses = await Task.WhenAll(
                PostWithIdempotency(first, cases[index].Path, cases[index].Body, key),
                PostWithIdempotency(second, cases[index].Path, cases[index].Body, key));
            Assert.IsTrue(responses.All(static response => response.StatusCode == HttpStatusCode.Created), cases[index].Path);
            int[] ids = await Task.WhenAll(responses.Select(ReadId));
            Assert.AreEqual(ids[0], ids[1], cases[index].Path);
            Assert.AreEqual(HttpStatusCode.Conflict,
                (await PostWithIdempotency(first, cases[index].Path, cases[index].Changed, key)).StatusCode,
                cases[index].Path);
        }


        string sharedKey = $"shared-{suffix}";
        using HttpClient otherUser = Client(secondFactory, subject: $"other-{suffix}");
        Assert.AreEqual(HttpStatusCode.Created, (await PostWithIdempotency(first, "/api/v1/products", new { sku = $"SHARED-A-{suffix}", name = "First", category = "Test", unitPrice = 1m, stockQuantity = 0, isActive = true }, sharedKey)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, (await PostWithIdempotency(otherUser, "/api/v1/products", new { sku = $"SHARED-B-{suffix}", name = "Second", category = "Test", unitPrice = 1m, stockQuantity = 0, isActive = true }, sharedKey)).StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, (await PostWithIdempotency(first, "/api/v1/carriers", new { code = $"SHARED-{suffix}", name = "Carrier", serviceLevel = "Ground", isActive = true }, sharedKey)).StatusCode);
    }

    [TestMethod]
    public async Task QuoteConversionCoversEverySourceStateAndSingletonReplayContract()
    {
        await using LiveApiFactory factory = CreateFactory();
        using HttpClient client = Client(factory);
        (ProductDto product, CustomerDto customer, AddressDto address, _) = await CreateMasters(client);
        string[][] rejectedSourcePaths = [[], ["sent"], ["sent", "rejected"], ["sent", "expired"]];
        foreach (string[] path in rejectedSourcePaths)
        {
            (QuoteDto quote, string etag) = await CreateQuote(client, product, customer, address);
            foreach (string step in path)
            {
                HttpResponseMessage transition = await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = step });
                etag = ETag(transition);
            }
            Assert.AreEqual(HttpStatusCode.Conflict, (await client.PutAsync($"/api/v1/quotes/{quote.Id}/sales-order", null)).StatusCode);
        }

        (QuoteDto deleted, string deletedEtag) = await CreateQuote(client, product, customer, address);
        using (HttpRequestMessage delete = new(HttpMethod.Delete, $"/api/v1/quotes/{deleted.Id}"))
        {
            delete.Headers.TryAddWithoutValidation("If-Match", deletedEtag);
            Assert.AreEqual(HttpStatusCode.NoContent, (await client.SendAsync(delete)).StatusCode);
        }
        Assert.AreEqual(HttpStatusCode.NotFound, (await client.PutAsync($"/api/v1/quotes/{deleted.Id}/sales-order", null)).StatusCode);

        (QuoteDto accepted, string acceptedEtag) = await CreateQuote(client, product, customer, address);
        HttpResponseMessage sent = await Transition(client, $"/api/v1/quotes/{accepted.Id}/status-transitions", acceptedEtag, new { status = "sent" });
        HttpResponseMessage acceptedResponse = await Transition(client, $"/api/v1/quotes/{accepted.Id}/status-transitions", ETag(sent), new { status = "accepted" });
        Assert.AreEqual(HttpStatusCode.OK, acceptedResponse.StatusCode);
        HttpResponseMessage created = await client.PutAsync($"/api/v1/quotes/{accepted.Id}/sales-order", null);
        HttpResponseMessage replay = await client.PutAsync($"/api/v1/quotes/{accepted.Id}/sales-order", null);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
        Assert.IsNotNull(created.Headers.Location);
        SalesOrderDto first = await Read<SalesOrderDto>(created);
        SalesOrderDto second = await Read<SalesOrderDto>(replay);
        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual(first.OrderNumber, second.OrderNumber);
        StringAssert.EndsWith(created.Headers.Location.ToString(), $"/api/v1/sales-orders/{first.Id}");
    }

    #endregion

    #region Private Methods

    private static LiveApiFactory CreateFactory() => new(RequiredConnectionString());

    private static string RequiredConnectionString()
    {
        string? connection = Environment.GetEnvironmentVariable("ConnectionStrings__DatabaseConnection");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Inconclusive("Set ConnectionStrings__DatabaseConnection to run authenticated API acceptance tests.");
        return connection!;
    }

    private static HttpClient Client(
        LiveApiFactory factory,
        string permission = "business.write",
        string subject = "specialist-subject",
        string displayName = "Acceptance User")
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(permission, subject, displayName, factory.TokenIssuer));
        return client;
    }

    private static async Task ExecuteSqlAsync(string connectionString, string commandText, params SqlParameter[] parameters)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(commandText, connection);
        command.Parameters.AddRange(parameters);
        _ = await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertQuoteTransition(
        HttpClient client,
        ProductDto product,
        CustomerDto customer,
        AddressDto address,
        string[] sourcePath,
        string target,
        HttpStatusCode expected)
    {
        (QuoteDto quote, string etag) = await CreateQuote(client, product, customer, address);
        foreach (string step in sourcePath)
        {
            HttpResponseMessage response = await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = step });
            etag = ETag(response);
        }
        Assert.AreEqual(expected, (await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = target })).StatusCode);
    }

    private static async Task<(SalesOrderDto Order, string ETag)> CreateAndAdvanceOrder(
        HttpClient client,
        ProductDto product,
        CustomerDto customer,
        AddressDto address,
        CarrierDto carrier,
        string[] path)
    {
        HttpResponseMessage created = await Post(client, "/api/v1/sales-orders", new
        {
            customerId = customer.Id,
            shippingAddressId = address.Id,
            requestedShipDate = "2026-08-15",
            notes = "matrix",
            lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = product.UnitPrice, discountPercent = 0m } },
        });
        SalesOrderDto order = await Read<SalesOrderDto>(created);
        string etag = ETag(created);
        foreach (string step in path)
        {
            HttpResponseMessage response = await Transition(client, $"/api/v1/sales-orders/{order.Id}/status-transitions", etag, TransitionBody(step, carrier.Id));
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, step);
            order = await Read<SalesOrderDto>(response);
            etag = ETag(response);
        }
        return (order, etag);
    }

    private static object TransitionBody(string status, int carrierId) => status == "shipped"
        ? (object)new { status, carrierId = (int?)carrierId, trackingNumber = "TRACK-MATRIX" }
        : new { status, carrierId = (int?)null, trackingNumber = (string?)null };

    private static async Task<int> ReadId(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, body);
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<T?> ScalarAsync<T>(string connectionString, string commandText, params SqlParameter[] parameters)
    {
        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new(commandText, connection);
        command.Parameters.AddRange(parameters);
        object? result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    private static async Task<(ProductDto, CustomerDto, AddressDto, CarrierDto)> CreateMasters(HttpClient client)
    {
        ProductDto product = await CreateProduct(client);
        CustomerDto customer = await CreateCustomer(client);
        AddressDto address = await CreateAddress(client, customer.Id, "Shipping", true);
        CarrierDto carrier = await CreateCarrier(client);
        return (product, customer, address, carrier);
    }

    private static async Task<ProductDto> CreateProduct(HttpClient client)
    {
        string suffix = Suffix();
        return await Read<ProductDto>(await Post(client, "/api/v1/products", new { sku = $"SKU-{suffix}", name = $"Product {suffix}", category = "Test", unitPrice = 10.005m, stockQuantity = 10, isActive = true }));
    }

    private static async Task<CustomerDto> CreateCustomer(HttpClient client)
    {
        string suffix = Suffix();
        return await Read<CustomerDto>(await Post(client, "/api/v1/customers", new { accountNumber = $"ACC-{suffix}", name = $"Customer {suffix}", email = $"{suffix}@example.test", creditLimit = 1000m, paymentTermsDays = 30, isActive = true }));
    }

    private static async Task<AddressDto> CreateAddress(HttpClient client, int customerId, string label, bool defaultShipping) =>
        await Read<AddressDto>(await Post(client, "/api/v1/addresses", new { customerId, type = "shipping", label, line1 = "1 Main St", city = "Beacon", postalCode = "1000", country = "US", defaultBilling = false, defaultShipping }));

    private static async Task<CarrierDto> CreateCarrier(HttpClient client)
    {
        string suffix = Suffix();
        return await Read<CarrierDto>(await Post(client, "/api/v1/carriers", new { code = $"CAR-{suffix}", name = "Carrier", serviceLevel = "Ground", trackingUrlTemplate = "https://tracking.example.test/{trackingNumber}", isActive = true }));
    }

    private static async Task<(QuoteDto Quote, string ETag)> CreateQuote(HttpClient client, ProductDto product, CustomerDto customer, AddressDto address)
    {
        HttpResponseMessage response = await Post(client, "/api/v1/quotes", new { customerId = customer.Id, shippingAddressId = address.Id, quoteDate = "2026-08-01", validUntil = "2026-08-31", notes = "acceptance", lines = new[] { new { productId = product.Id, quantity = 1, unitPrice = 10.005m, discountPercent = 0m } } });
        return (await Read<QuoteDto>(response), ETag(response));
    }

    private static async Task<QuoteDto> CreateAcceptedQuote(HttpClient client)
    {
        (ProductDto product, CustomerDto customer, AddressDto address, _) = await CreateMasters(client);
        (QuoteDto quote, string etag) = await CreateQuote(client, product, customer, address);
        HttpResponseMessage sent = await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", etag, new { status = "sent" });
        return await Read<QuoteDto>(await Transition(client, $"/api/v1/quotes/{quote.Id}/status-transitions", ETag(sent), new { status = "accepted" }));
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string path, object body) => client.PostAsJsonAsync(path, body, Json);

    private static async Task<HttpResponseMessage> PostWithIdempotency(HttpClient client, string path, object body, string key)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> Put(HttpClient client, string path, object body, string etag)
    {
        using HttpRequestMessage request = new(HttpMethod.Put, path) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> Transition(HttpClient client, string path, string etag, object body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<T>(body, Json) ?? throw new AssertFailedException("Response body was empty.");
    }

    private static async Task<T> GetWithETag<T>(HttpClient client, string path) => await Read<T>(await client.GetAsync(path));

    private static string ETag(HttpResponseMessage response) => response.Headers.ETag?.Tag ?? throw new AssertFailedException("ETag was missing.");

    private static string Suffix() => Guid.NewGuid().ToString("N")[..10];

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
        return options;
    }

    private static string Token(string permission, string subject, string displayName, string tokenIssuer)
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = tokenIssuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim("tid", "beacon-acceptance-tests"),
                new Claim("oid", "00000000-0000-0000-0000-000000000005"),
                new Claim("sub", subject),
                new Claim("idtyp", "user"),
                new Claim("name", displayName),
                new Claim("scp", permission),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256),
        };
        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    #endregion

    #region Nested Types

    private sealed class LiveApiFactory(string connectionString, string tokenIssuer = Issuer) : WebApplicationFactory<Program>
    {
        #region Properties

        public string TokenIssuer => tokenIssuer;

        #endregion

        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
            builder.UseSetting("AzureAd:TenantId", "beacon-acceptance-tests");
            builder.UseSetting("AzureAd:ClientId", Audience);
            builder.UseSetting("Authentication:Audience", Audience);
            builder.UseSetting("Authentication:Issuer", tokenIssuer);
            builder.UseSetting("Authentication:Permissions:Read", "business.read");
            builder.UseSetting("Authentication:Permissions:Write", "business.write");
            builder.UseSetting("ConnectionStrings:DatabaseConnection", connectionString);
            builder.ConfigureTestServices(services =>
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, ConfigureTestBearer));
        }

        private void ConfigureTestBearer(JwtBearerOptions options)
        {
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SigningKey));
            options.Authority = null;
            options.Configuration = new OpenIdConnectConfiguration { Issuer = tokenIssuer };
            options.Configuration.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
            options.TokenValidationParameters.IssuerSigningKey = key;
            options.TokenValidationParameters.ValidIssuer = tokenIssuer;
            options.TokenValidationParameters.ValidAudience = Audience;
        }

        #endregion
    }

    #endregion
}
