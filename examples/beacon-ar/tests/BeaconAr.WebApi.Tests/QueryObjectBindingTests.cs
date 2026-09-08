using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.MasterData.Entities;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Domain.Sales.Entities;
using BeaconAr.Providers.Access;
using BeaconAr.Providers.MasterData;
using BeaconAr.Providers.Sales;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using QuoteStatus = BeaconAr.Interfaces.Sales.Enums.QuoteStatus;
using SalesOrderStatus = BeaconAr.Interfaces.Sales.Enums.SalesOrderStatus;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class QueryObjectBindingTests
{
    #region Nested Types

    private sealed class QueryBindingFactory : WebApplicationFactory<Program>
    {
        #region Properties

        public ProductSearchRequest? Product { get; set; }

        public CustomerSearchRequest? Customer { get; set; }

        public CarrierSearchRequest? Carrier { get; set; }

        public AddressSearchRequest? Address { get; set; }

        public QuoteSearchRequest? Quote { get; set; }

        public SalesOrderSearchRequest? SalesOrder { get; set; }

        public int SearchCalls { get; set; }

        #endregion

        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
            builder.UseSetting("AzureAd:TenantId", "beacon-query-binding-tests");
            builder.UseSetting("AzureAd:ClientId", BeaconArTestTokenFactory.Audience);
            builder.UseSetting("Authentication:Audience", BeaconArTestTokenFactory.Audience);
            builder.UseSetting("Authentication:Issuer", BeaconArTestTokenFactory.Issuer);
            builder.UseSetting("Authentication:Permissions:Read", "business.read");
            builder.UseSetting("Authentication:Permissions:Write", "business.write");
            builder.UseSetting("ConnectionStrings:DatabaseConnection",
                "Server=(local);Database=BeaconArQueryBindingTests;Integrated Security=true;TrustServerCertificate=true");
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, ConfigureTestBearer);
                services.RemoveAll<ICurrentUserProvider>();
                services.RemoveAll<IProductProvider>();
                services.RemoveAll<ICustomerProvider>();
                services.RemoveAll<ICarrierProvider>();
                services.RemoveAll<IAddressProvider>();
                services.RemoveAll<IQuoteProvider>();
                services.RemoveAll<ISalesOrderProvider>();
                services.AddScoped<ICurrentUserProvider, FakeCurrentUserProvider>();
                services.AddScoped<IProductProvider>(_ => new FakeProductProvider(this));
                services.AddScoped<ICustomerProvider>(_ => new FakeCustomerProvider(this));
                services.AddScoped<ICarrierProvider>(_ => new FakeCarrierProvider(this));
                services.AddScoped<IAddressProvider>(_ => new FakeAddressProvider(this));
                services.AddScoped<IQuoteProvider>(_ => new FakeQuoteProvider(this));
                services.AddScoped<ISalesOrderProvider>(_ => new FakeSalesOrderProvider(this));
            });
        }

        #endregion

        #region Private Methods

        private static void ConfigureTestBearer(JwtBearerOptions options)
        {
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(BeaconArTestTokenFactory.SigningKey));
            options.Authority = null;
            options.Configuration = new OpenIdConnectConfiguration { Issuer = BeaconArTestTokenFactory.Issuer };
            options.Configuration.SigningKeys.Add(key);
            options.ConfigurationManager =
                new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
            options.TokenValidationParameters.IssuerSigningKey = key;
            options.TokenValidationParameters.ValidIssuer = BeaconArTestTokenFactory.Issuer;
            options.TokenValidationParameters.ValidAudience = BeaconArTestTokenFactory.Audience;
        }

        #endregion
    }

    private sealed class FakeCurrentUserProvider : ICurrentUserProvider
    {
        #region Public Methods

        public Task<CurrentUserDto> ResolveAsync(AuthenticatedIdentity identity, CancellationToken cancellationToken) =>
            Task.FromResult(new CurrentUserDto(42, identity.DisplayName, identity.Email, identity.Policies));

        #endregion
    }

    private sealed class FakeProductProvider(QueryBindingFactory factory) : IProductProvider
    {
        #region Public Methods

        public Task<PageResult<ProductView>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
        {
            request.Validate("id", "sku", "name");
            factory.Product = request;
            factory.SearchCalls++;
            return Empty<ProductView>(request.PageNumber, request.PageSize);
        }

        public Task<ProductView> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Unsupported();
        public Task<ProductView> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken) => throw Unsupported();
        public Task<ProductView> UpdateAsync(int id, ProductUpdateRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();

        #endregion
    }

    private sealed class FakeCustomerProvider(QueryBindingFactory factory) : ICustomerProvider
    {
        #region Public Methods

        public Task<PageResult<CustomerView>> SearchAsync(CustomerSearchRequest request, CancellationToken cancellationToken)
        {
            request.Validate("id", "name");
            factory.Customer = request;
            factory.SearchCalls++;
            return Empty<CustomerView>(request.PageNumber, request.PageSize);
        }

        public Task<CustomerView> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Unsupported();
        public Task<CustomerView> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken) => throw Unsupported();
        public Task<CustomerView> UpdateAsync(int id, CustomerUpdateRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();

        #endregion
    }

    private sealed class FakeCarrierProvider(QueryBindingFactory factory) : ICarrierProvider
    {
        #region Public Methods

        public Task<PageResult<CarrierView>> SearchAsync(CarrierSearchRequest request, CancellationToken cancellationToken)
        {
            request.Validate("id", "name");
            factory.Carrier = request;
            factory.SearchCalls++;
            return Empty<CarrierView>(request.PageNumber, request.PageSize);
        }

        public Task<CarrierView> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Unsupported();
        public Task<CarrierView> CreateAsync(CarrierCreateRequest request, CancellationToken cancellationToken) => throw Unsupported();
        public Task<CarrierView> UpdateAsync(int id, CarrierUpdateRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();

        #endregion
    }

    private sealed class FakeAddressProvider(QueryBindingFactory factory) : IAddressProvider
    {
        #region Public Methods

        public Task<PageResult<CustomerAddressView>> SearchAsync(AddressSearchRequest request, CancellationToken cancellationToken)
        {
            request.Validate("id", "name");
            factory.Address = request;
            factory.SearchCalls++;
            return Empty<CustomerAddressView>(request.PageNumber, request.PageSize);
        }

        public Task<CustomerAddressView> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Unsupported();
        public Task<CustomerAddressView> CreateAsync(AddressCreateRequest request, CancellationToken cancellationToken) => throw Unsupported();
        public Task<CustomerAddressView> UpdateAsync(int id, AddressUpdateRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();

        #endregion
    }

    private sealed class FakeQuoteProvider(QueryBindingFactory factory) : IQuoteProvider
    {
        #region Public Methods

        public Task<PageResult<QuoteView>> SearchAsync(QuoteSearchRequest request, CancellationToken cancellationToken)
        {
            request.Validate();
            factory.Quote = request;
            factory.SearchCalls++;
            return Empty<QuoteView>(request.PageNumber, request.PageSize);
        }

        public Task<QuoteDto> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Unsupported();
        public Task<QuoteDto> CreateAsync(QuoteCreateRequest request, CancellationToken cancellationToken) => throw Unsupported();
        public Task<QuoteDto> UpdateAsync(int id, QuoteUpdateRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task<QuoteDto> TransitionAsync(int id, QuoteStatusTransitionRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();

        #endregion
    }

    private sealed class FakeSalesOrderProvider(QueryBindingFactory factory) : ISalesOrderProvider
    {
        #region Public Methods

        public Task<PageResult<SalesOrderView>> SearchAsync(SalesOrderSearchRequest request, CancellationToken cancellationToken)
        {
            request.Validate();
            factory.SalesOrder = request;
            factory.SearchCalls++;
            return Empty<SalesOrderView>(request.PageNumber, request.PageSize);
        }

        public Task<SalesOrderDto> GetByIdAsync(int id, CancellationToken cancellationToken) => throw Unsupported();
        public Task<SalesOrderDto> CreateDirectAsync(SalesOrderCreateRequest request, CancellationToken cancellationToken) => throw Unsupported();
        public Task<SalesOrderDto> UpdateAsync(int id, SalesOrderUpdateRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task DeleteAsync(int id, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();
        public Task<SalesOrderDto> TransitionAsync(int id, SalesOrderStatusTransitionRequest request, string expectedVersion, CancellationToken cancellationToken) => throw Unsupported();

        #endregion
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task EmptyQueriesBindEveryRequestDefaultThroughMvc()
    {
        await using QueryBindingFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);

        foreach (string path in new[] { "products", "customers", "carriers", "addresses", "quotes", "sales-orders" })
            Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/{path}")).StatusCode, path);

        AssertMasterDefaults(factory.Product);
        AssertMasterDefaults(factory.Customer);
        AssertMasterDefaults(factory.Carrier);
        AssertMasterDefaults(factory.Address);
        AssertSalesDefaults(factory.Quote);
        AssertSalesDefaults(factory.SalesOrder);
        Assert.AreEqual(6, factory.SearchCalls);
    }

    [TestMethod]
    public async Task CamelCaseQueryKeysBindEveryOptionalFilterAndEnumThroughMvc()
    {
        await using QueryBindingFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(
            "/api/v1/products?search=widget&pageNumber=2&pageSize=11&sortField=sku&sortDirection=desc&active=true")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(
            "/api/v1/customers?search=acme&pageNumber=3&pageSize=12&sortField=name&sortDirection=desc&active=false")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(
            "/api/v1/carriers?search=fast&pageNumber=4&pageSize=13&sortField=name&sortDirection=desc&active=true")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(
            "/api/v1/addresses?search=dock&pageNumber=5&pageSize=14&sortField=name&sortDirection=desc&customerId=7&type=shipping&usage=shipping")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(
            "/api/v1/quotes?search=Q-1&status=sent&customerId=8&pageNumber=6&pageSize=15&sortField=quoteDate&sortDirection=asc")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(
            "/api/v1/sales-orders?search=SO-1&status=confirmed&customerId=9&sourceQuoteId=10&pageNumber=7&pageSize=16&sortField=status&sortDirection=asc")).StatusCode);

        Assert.AreEqual(("widget", 2, 11, "sku", SortDirection.Desc, true), MasterValues(factory.Product!));
        Assert.AreEqual(("acme", 3, 12, "name", SortDirection.Desc, false), MasterValues(factory.Customer!));
        Assert.AreEqual(("fast", 4, 13, "name", SortDirection.Desc, true), MasterValues(factory.Carrier!));
        Assert.AreEqual(("dock", 5, 14, "name", SortDirection.Desc),
            (factory.Address!.Search, factory.Address.PageNumber, factory.Address.PageSize, factory.Address.SortField, factory.Address.SortDirection));
        Assert.AreEqual(7, factory.Address.CustomerId);
        Assert.AreEqual("shipping", factory.Address.Type);
        Assert.AreEqual(AddressUsage.Shipping, factory.Address.Usage);
        Assert.AreEqual(("Q-1", QuoteStatus.Sent, 8, 6, 15, "quoteDate", SortDirection.Asc),
            (factory.Quote!.Search, factory.Quote.Status, factory.Quote.CustomerId, factory.Quote.PageNumber,
                factory.Quote.PageSize, factory.Quote.SortField, factory.Quote.SortDirection));
        Assert.AreEqual(("SO-1", SalesOrderStatus.Confirmed, 9, 10, 7, 16, "status", SortDirection.Asc),
            (factory.SalesOrder!.Search, factory.SalesOrder.Status, factory.SalesOrder.CustomerId,
                factory.SalesOrder.SourceQuoteId, factory.SalesOrder.PageNumber, factory.SalesOrder.PageSize,
                factory.SalesOrder.SortField, factory.SalesOrder.SortDirection));
    }

    [TestMethod]
    public async Task InvalidBindingAndRequestRulesKeepStableProblemDetails()
    {
        await using QueryBindingFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);

        await AssertProblem(client, "/api/v1/products?sortDirection=sideways", "validation_failed", "sortDirection");
        await AssertProblem(client, "/api/v1/products?pageSize=101", "validation_failed", "pageSize");
        await AssertProblem(client, "/api/v1/customers?pageNumber=0", "validation_failed", "pageNumber");
        await AssertProblem(client, "/api/v1/carriers?sortField=unknown", "validation_failed", "sortField");
        await AssertProblem(client, "/api/v1/addresses?customerId=0", "validation_failed", "customerId");
        await AssertProblem(client, "/api/v1/quotes?status=unknown", "validation_failed", "status");
        await AssertProblem(client, "/api/v1/quotes?customerId=0", "validation_failed", "customerId");
        await AssertProblem(client, "/api/v1/sales-orders?sourceQuoteId=0", "validation_failed", "sourceQuoteId");
    }

    [TestMethod]
    public async Task SearchEndpointsRequireReadAuthorizationBeforeProviderExecution()
    {
        await using QueryBindingFactory factory = new();
        using HttpClient client = factory.CreateClient();

        foreach (string path in new[] { "products", "customers", "carriers", "addresses", "quotes", "sales-orders" })
            Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/v1/{path}")).StatusCode, path);
        Assert.AreEqual(0, factory.SearchCalls);
    }

    #endregion

    #region Private Methods

    private static HttpClient AuthorizedClient(WebApplicationFactory<Program> factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", BeaconArTestTokenFactory.Create(scope: "business.read"));
        return client;
    }

    private static Task<PageResult<T>> Empty<T>(int pageNumber, int pageSize) =>
        Task.FromResult(new PageResult<T>([], pageNumber, pageSize, 0, 0));

    private static NotSupportedException Unsupported() => new("The fake supports search only.");

    private static void AssertMasterDefaults(MasterDataSearchRequest? request)
    {
        Assert.IsNotNull(request);
        Assert.IsNull(request.Search);
        Assert.AreEqual(1, request.PageNumber);
        Assert.AreEqual(10, request.PageSize);
        Assert.IsNull(request.SortField);
        Assert.AreEqual(SortDirection.Asc, request.SortDirection);
    }

    private static void AssertSalesDefaults(QuoteSearchRequest? request)
    {
        Assert.IsNotNull(request);
        Assert.IsNull(request.Search);
        Assert.IsNull(request.Status);
        Assert.IsNull(request.CustomerId);
        Assert.AreEqual(1, request.PageNumber);
        Assert.AreEqual(10, request.PageSize);
        Assert.IsNull(request.SortField);
        Assert.AreEqual(SortDirection.Desc, request.SortDirection);
    }

    private static void AssertSalesDefaults(SalesOrderSearchRequest? request)
    {
        Assert.IsNotNull(request);
        Assert.IsNull(request.Search);
        Assert.IsNull(request.Status);
        Assert.IsNull(request.CustomerId);
        Assert.IsNull(request.SourceQuoteId);
        Assert.AreEqual(1, request.PageNumber);
        Assert.AreEqual(10, request.PageSize);
        Assert.IsNull(request.SortField);
        Assert.AreEqual(SortDirection.Desc, request.SortDirection);
    }

    private static (string?, int, int, string?, SortDirection, bool?) MasterValues(MasterDataSearchRequest request) =>
        (request.Search, request.PageNumber, request.PageSize, request.SortField, request.SortDirection,
            request switch
            {
                ProductSearchRequest value => value.Active,
                CustomerSearchRequest value => value.Active,
                CarrierSearchRequest value => value.Active,
                _ => null,
            });

    private static async Task AssertProblem(HttpClient client, string path, string code, string field)
    {
        HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, body);
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.AreEqual(code, document.RootElement.GetProperty("code").GetString(), body);
        Assert.IsTrue(document.RootElement.GetProperty("errors").TryGetProperty(field, out _), body);
    }

    #endregion
}
