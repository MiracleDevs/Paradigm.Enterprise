using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.MasterData.Contracts;
using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Domain.Sales;
using BeaconAr.Domain.Sales.Contracts;
using BeaconAr.Providers.Access;
using BeaconAr.Providers.Reporting;
using BeaconAr.WebApi;
using BeaconAr.WebApi.Controllers;
using BeaconAr.WebApi.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using CustomerAddressView = BeaconAr.Domain.Receivables.Entities.CustomerAddressView;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class BeaconArApiContractTests
{
    #region Constants

    private const string Audience = "api://beacon-tests";
    private const string Issuer = "https://issuer.beacon.test";
    private const string SigningKey = "beacon-ar-contract-tests-signing-key-2026";

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task Scenario01MissingTokenReturnsSafeProblem()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(ApiContract.ProblemJson, response.Content.Headers.ContentType?.MediaType);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "\"code\":\"unauthorized\"");
    }

    [TestMethod]
    public async Task Scenario02InvalidSignatureReturnsUnauthorized()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("business.read", signingKey: SigningKey + "wrong"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Scenario03WrongAudienceReturnsUnauthorized()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken("business.read", audience: "api://other"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/me");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Scenario04ReadOnlyUserCanReadProfile()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory, "business.read");

        HttpResponseMessage response = await client.GetAsync("/api/v1/me");
        string body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(body, "\"displayName\":\"Contract User\"");
        StringAssert.Contains(body, "\"business.read\"");
    }

    [TestMethod]
    public async Task Scenario05ReadOnlyUserCannotMutate()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory, "business.read");
        using StringContent body = new("{}", Encoding.UTF8, ApiContract.Json);

        HttpResponseMessage response = await client.PostAsync("/api/v1/products", body);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "\"code\":\"forbidden\"");
    }

    [TestMethod]
    public async Task Scenario06WritePermissionImpliesRead()
    {
        await using TestApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory, "business.write");

        HttpResponseMessage response = await client.GetAsync("/api/v1/dashboard/summary");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "\"openQuotes\":4");
    }

    [TestMethod]
    public void Scenario07StrongEntityTagsRoundTrip()
    {
        HeaderDictionary headers = new() { ["If-Match"] = ETagCodec.Format("AQIDBA==") };

        Assert.AreEqual("AQIDBA==", ETagCodec.ParseRequired(headers));
    }

    [TestMethod]
    public void Scenario08InvalidEntityTagsAreRejected()
    {
        foreach (string value in new[] { "*", "W/\"AQID\"", "AQID", "\"one\", \"two\"" })
        {
            HeaderDictionary headers = new() { ["If-Match"] = value };
            ApiBoundaryException exception = Assert.ThrowsExactly<ApiBoundaryException>(() => ETagCodec.ParseRequired(headers));
            Assert.AreEqual("invalid_if_match", exception.Code);
        }
    }

    [TestMethod]
    public void Scenario09JsonContractUsesCamelCaseStringEnumsAndDates()
    {
        QuoteSummaryDto value = new(1, "Q-0001", 2, "A-1", "Customer", new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31), QuoteStatus.Accepted, 12.3456m, 0m, 12.3456m, null, 42,
            new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero), null, null, "AQID");
        string json = JsonSerializer.Serialize(value, BeaconArApiJsonContext.Default.Options);

        StringAssert.Contains(json, "\"quoteDate\":\"2026-08-01\"");
        StringAssert.Contains(json, "\"status\":\"accepted\"");
        StringAssert.Contains(json, "\"subtotal\":12.3456");
        StringAssert.Contains(json, "\"creationDate\":\"2026-08-01T12:00:00.0000000Z\"");
    }

    [TestMethod]
    public async Task GeneratedMasterViewsUseStableTransportAliasesAndExpandedFields()
    {
        await using TestApiFactory factory = new();
        JsonSerializerOptions options = factory.Services
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()
            .Value.JsonSerializerOptions;
        CustomerAddressView value = new()
        {
            Id = 9,
            CustomerId = 4,
            CustomerAccountNumber = "ACME-001",
            CustomerName = "Acme Distribution",
            AddressTypeId = 2,
            AddressTypeCode = "shipping",
            AddressTypeDisplayName = "Shipping",
            Label = "Warehouse",
            Line1 = "100 Beacon Way",
            City = "Seattle",
            PostalCode = "98101",
            Country = "US",
            IsDefaultBilling = false,
            IsDefaultShipping = true,
            CreationDate = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
            RowVersion = [1, 2, 3, 4],
        };

        string json = JsonSerializer.Serialize(value, options);
        StringAssert.Contains(json, "\"customerAccountNumber\":\"ACME-001\"");
        StringAssert.Contains(json, "\"customerName\":\"Acme Distribution\"");
        StringAssert.Contains(json, "\"addressTypeDisplayName\":\"Shipping\"");
        StringAssert.Contains(json, "\"type\":\"shipping\"");
        StringAssert.Contains(json, "\"defaultBilling\":false");
        StringAssert.Contains(json, "\"defaultShipping\":true");
        StringAssert.Contains(json, "\"version\":\"AQIDBA==\"");
        Assert.IsFalse(json.Contains("rowVersion", StringComparison.Ordinal));
        Assert.IsFalse(json.Contains("addressTypeCode", StringComparison.Ordinal));

        CustomerAddressView? roundTrip = JsonSerializer.Deserialize<CustomerAddressView>(json, options);
        Assert.IsNotNull(roundTrip);
        Assert.AreEqual("shipping", roundTrip.AddressTypeCode);
        Assert.IsTrue(roundTrip.IsDefaultShipping);
        CollectionAssert.AreEqual(value.RowVersion, roundTrip.RowVersion);
    }

    [TestMethod]
    public void Scenario10EndpointCatalogIsVersionedNamedAndProtected()
    {
        Type[] controllers =
        [
            typeof(MeController), typeof(DashboardController), typeof(ProductsController), typeof(CustomersController),
            typeof(AddressesController), typeof(CarriersController), typeof(QuotesController), typeof(SalesOrdersController),
        ];
        List<string> operationNames = [];
        int operationCount = 0;
        foreach (Type controller in controllers)
        {
            Assert.IsTrue(controller.IsSealed);
            Assert.IsTrue(typeof(ControllerBase).IsAssignableFrom(controller));
            Assert.IsFalse(controller.GetCustomAttributes<AllowAnonymousAttribute>(true).Any());
            Assert.IsTrue(controller.GetCustomAttributes<AuthorizeAttribute>(true).Any());
            RouteAttribute route = controller.GetCustomAttribute<RouteAttribute>()!;
            StringAssert.StartsWith(route.Template, "api/v1/");
            foreach (MethodInfo method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                HttpMethodAttribute? verb = method.GetCustomAttributes<HttpMethodAttribute>().SingleOrDefault();
                if (verb is null)
                    continue;
                operationCount++;
                Assert.IsFalse(string.IsNullOrWhiteSpace(verb.Name));
                operationNames.Add(verb.Name!);
            }
        }

        Assert.AreEqual(35, operationCount);
        Assert.AreEqual(operationNames.Count, operationNames.Distinct(StringComparer.Ordinal).Count());
    }

    #endregion

    #region Private Methods

    private static HttpClient AuthorizedClient(TestApiFactory factory, string permission)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(permission));
        return client;
    }

    private static string CreateToken(string permission, string audience = Audience, string signingKey = SigningKey)
    {
        JwtSecurityTokenHandler handler = new();
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity(
            [
                new Claim("tid", "beacon-contract-tests"),
                new Claim("oid", "00000000-0000-0000-0000-000000000003"),
                new Claim("sub", "contract-user"),
                new Claim("idtyp", "user"),
                new Claim("name", "Contract User"),
                new Claim("email", "contract@example.test"),
                new Claim("scp", permission),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };
        return handler.CreateEncodedJwt(descriptor);
    }

    #endregion

    #region Nested Types

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
            builder.UseSetting("AzureAd:TenantId", "beacon-contract-tests");
            builder.UseSetting("AzureAd:ClientId", Audience);
            builder.UseSetting("Authentication:Audience", Audience);
            builder.UseSetting("Authentication:Issuer", Issuer);
            builder.UseSetting("Authentication:Permissions:Read", "business.read");
            builder.UseSetting("Authentication:Permissions:Write", "business.write");
            builder.UseSetting("ConnectionStrings:DatabaseConnection", "Server=(local);Database=BeaconArTests;Integrated Security=true;TrustServerCertificate=true");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Audience"] = Audience,
                ["Authentication:Issuer"] = Issuer,
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "beacon-contract-tests",
                ["AzureAd:ClientId"] = Audience,
                ["Authentication:Permissions:Read"] = "business.read",
                ["Authentication:Permissions:Write"] = "business.write",
                ["ConnectionStrings:DatabaseConnection"] = "Server=(local);Database=BeaconArTests;Integrated Security=true;TrustServerCertificate=true",
            }));
            builder.ConfigureTestServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, ConfigureTestBearer);
                services.RemoveAll<ICurrentUserProvider>();
                services.RemoveAll<IDashboardProvider>();
                services.AddScoped<ICurrentUserProvider, FakeCurrentUserProvider>();
                services.AddScoped<IDashboardProvider, FakeDashboardProvider>();
            });
        }

        private static void ConfigureTestBearer(JwtBearerOptions options)
        {
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(SigningKey));
            options.Authority = null;
            options.Configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
            options.Configuration.SigningKeys.Add(key);
            options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
            options.TokenValidationParameters.IssuerSigningKey = key;
            options.TokenValidationParameters.ValidIssuer = Issuer;
            options.TokenValidationParameters.ValidAudience = Audience;
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

    private sealed class FakeDashboardProvider : IDashboardProvider
    {
        #region Public Methods

        public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DashboardSummaryDto(1, 2, 3, 4, 5, new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)));

        #endregion
    }

    #endregion
}
