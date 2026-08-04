using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Operations.Contracts;
using BeaconAr.Providers.Access;
using BeaconAr.Providers.Operations;
using BeaconAr.WebApi.Controllers;
using BeaconAr.WebApi.Http;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class BeaconArApiHostHardeningTests
{
    #region Constants

    private const string Audience = "api://beacon-host-tests";
    private const string Issuer = "https://issuer.beacon.host.test";
    private const string SigningKey = "beacon-ar-host-tests-signing-key-2026";

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task ProductionRejectsSymmetricSigningKeyConfiguration()
    {
        await using ProductionSigningKeyFactory factory = new();

        Exception exception = await Assert.ThrowsAsync<Exception>(() => Task.Run(() => _ = factory.Server));

        StringAssert.Contains(AllMessages(exception), "Authentication:SigningKey is permitted only");
    }

    [TestMethod]
    [DataRow("AzureAd:Instance")]
    [DataRow("AzureAd:TenantId")]
    [DataRow("AzureAd:ClientId")]
    [DataRow("Cors:AllowedOrigins")]
    public async Task ProductionRejectsEachMissingRequiredIdentityOrCorsSetting(string missingSetting)
    {
        await using ProductionConfigurationFactory factory = new(missingSetting);

        Exception exception = await Assert.ThrowsAsync<Exception>(() => Task.Run(() => _ = factory.Server));

        StringAssert.Contains(
            AllMessages(exception),
            "AzureAd instance, tenant ID, client ID, and at least one CORS origin are required");
    }

    [TestMethod]
    public void ProductionAcceptsCompleteIdentityAndCorsConfigurationWithoutMetadataCalls()
    {
        using ProductionConfigurationFactory factory = new();

        _ = factory.Server;
    }

    [TestMethod]
    public void ProductionGraphConstructsEveryBusinessController()
    {
        using DependencyGraphFactory factory = new();
        using IServiceScope scope = factory.Services.CreateScope();
        Type[] controllerTypes =
        [
            typeof(MeController), typeof(DashboardController), typeof(ProductsController), typeof(CustomersController),
            typeof(AddressesController), typeof(CarriersController), typeof(QuotesController), typeof(SalesOrdersController),
        ];

        foreach (Type controllerType in controllerTypes)
            _ = ActivatorUtilities.CreateInstance(scope.ServiceProvider, controllerType);
    }

    [TestMethod]
    public async Task ApiFrameworkFailuresUseStableProblemDetails()
    {
        await using HardenedApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);
        using HttpRequestMessage unsupported = new(HttpMethod.Post, "/api/v1/products")
        {
            Content = new StringContent("not-json", Encoding.UTF8, "text/plain"),
        };

        await AssertProblem(await client.GetAsync("/api/v1/does-not-exist"), HttpStatusCode.NotFound, "route_not_found");
        await AssertProblem(await client.PutAsync("/api/v1/me", null), HttpStatusCode.MethodNotAllowed, "method_not_allowed");
        await AssertProblem(await client.SendAsync(unsupported), HttpStatusCode.UnsupportedMediaType, "unsupported_media_type");
    }

    [TestMethod]
    public async Task OversizedAndMalformedJsonAreNormalized()
    {
        await using HardenedApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);
        KestrelServerOptions limits = factory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;
        Assert.AreEqual(ApiContract.MaxJsonRequestBodySize, limits.Limits.MaxRequestBodySize);

        using StringContent oversizedBody = new(new string('x', checked((int)ApiContract.MaxJsonRequestBodySize + 1)), Encoding.UTF8, ApiContract.Json);
        await AssertProblem(await client.PostAsync("/api/v1/addresses", oversizedBody), HttpStatusCode.RequestEntityTooLarge, "request_too_large");

        using StringContent malformedBody = new("{\"sku\":", Encoding.UTF8, ApiContract.Json);
        await AssertProblem(await client.PostAsync("/api/v1/products", malformedBody), HttpStatusCode.BadRequest, "malformed_request");
    }

    [TestMethod]
    public async Task EveryConditionalMutationRejectsMissingPreconditionBeforeProviderExecution()
    {
        await using HardenedApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);
        (HttpMethod Method, string Path, string? Body)[] operations =
        [
            (HttpMethod.Put, "/api/v1/products/1", "{}"), (HttpMethod.Delete, "/api/v1/products/1", null),
            (HttpMethod.Put, "/api/v1/customers/1", "{}"), (HttpMethod.Delete, "/api/v1/customers/1", null),
            (HttpMethod.Put, "/api/v1/addresses/1", "{}"), (HttpMethod.Delete, "/api/v1/addresses/1", null),
            (HttpMethod.Put, "/api/v1/carriers/1", "{}"), (HttpMethod.Delete, "/api/v1/carriers/1", null),
            (HttpMethod.Put, "/api/v1/quotes/1", "{\"customerId\":1,\"shippingAddressId\":1,\"quoteDate\":\"2026-08-01\",\"validUntil\":\"2026-08-31\",\"lines\":[{\"productId\":1,\"quantity\":1,\"unitPrice\":1,\"discountPercent\":0}]}"), (HttpMethod.Delete, "/api/v1/quotes/1", null),
            (HttpMethod.Post, "/api/v1/quotes/1/status-transitions", "{\"status\":\"sent\"}"),
            (HttpMethod.Put, "/api/v1/sales-orders/1", "{\"customerId\":1,\"shippingAddressId\":1,\"lines\":[{\"productId\":1,\"quantity\":1,\"unitPrice\":1,\"discountPercent\":0}]}"), (HttpMethod.Delete, "/api/v1/sales-orders/1", null),
            (HttpMethod.Post, "/api/v1/sales-orders/1/status-transitions", "{\"status\":\"confirmed\"}"),
        ];

        foreach ((HttpMethod method, string path, string? body) in operations)
        {
            using HttpRequestMessage request = new(method, path);
            if (body is not null)
                request.Content = new StringContent(body, Encoding.UTF8, ApiContract.Json);
            await AssertProblem(await client.SendAsync(request), (HttpStatusCode)428, "precondition_required");
        }
    }

    [TestMethod]
    public async Task BindingAndUnexpectedErrorsRemainSafeAndCorrelated()
    {
        await using HardenedApiFactory factory = new();
        using HttpClient client = AuthorizedClient(factory);
        await AssertProblem(await client.GetAsync("/api/v1/products/0"), HttpStatusCode.BadRequest, "invalid_query");
        await AssertProblem(await client.GetAsync("/api/v1/products?sortDirection=sideways"), HttpStatusCode.BadRequest, "validation_failed");
        using StringContent empty = new(string.Empty, Encoding.UTF8, ApiContract.Json);
        await AssertProblem(await client.PostAsync("/api/v1/products", empty), HttpStatusCode.BadRequest, "validation_failed");

        await using UnexpectedErrorApiFactory unexpectedFactory = new();
        using HttpClient unexpectedClient = AuthorizedClient(unexpectedFactory);
        HttpResponseMessage unexpected = await unexpectedClient.GetAsync("/api/v1/dashboard/summary");
        string body = await unexpected.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.InternalServerError, unexpected.StatusCode);
        StringAssert.Contains(body, "\"code\":\"internal_error\"");
        Assert.IsFalse(body.Contains("sensitive-database-detail", StringComparison.Ordinal));
        Assert.IsFalse(body.Contains("System.InvalidOperationException", StringComparison.Ordinal));
    }

    #endregion

    #region Private Methods

    private static string AllMessages(Exception exception) =>
        string.Join(" | ", EnumerateExceptions(exception).Select(static value => value.Message));

    private static HttpClient AuthorizedClient(WebApplicationFactory<Program> factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());
        return client;
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

    private static string CreateToken()
    {
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim("tid", "beacon-host-tests"),
                new Claim("oid", "00000000-0000-0000-0000-000000000004"),
                new Claim("sub", "host-test-user"),
                new Claim("idtyp", "user"),
                new Claim("name", "Host Test User"),
                new Claim("scp", "business.write"),
            ]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256),
        };
        return new JwtSecurityTokenHandler().CreateEncodedJwt(descriptor);
    }

    private static IEnumerable<Exception> EnumerateExceptions(Exception exception)
    {
        yield return exception;
        if (exception is AggregateException aggregate)
        {
            foreach (Exception inner in aggregate.InnerExceptions.SelectMany(EnumerateExceptions))
                yield return inner;
        }
        else if (exception.InnerException is not null)
        {
            foreach (Exception inner in EnumerateExceptions(exception.InnerException))
                yield return inner;
        }
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(expectedStatus, response.StatusCode, body);
        Assert.AreEqual(ApiContract.ProblemJson, response.Content.Headers.ContentType?.MediaType, body);
        StringAssert.Contains(body, $"\"code\":\"{expectedCode}\"");
        StringAssert.Contains(body, "\"correlationId\":");
    }

    private static void ConfigureTestingHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
        builder.UseSetting("AzureAd:TenantId", "beacon-host-tests");
        builder.UseSetting("AzureAd:ClientId", Audience);
        builder.UseSetting("Authentication:Audience", Audience);
        builder.UseSetting("Authentication:Issuer", Issuer);
        builder.UseSetting("Authentication:Permissions:Read", "business.read");
        builder.UseSetting("Authentication:Permissions:Write", "business.write");
        builder.UseSetting("ConnectionStrings:DatabaseConnection", "Server=(local);Database=BeaconArHostTests;Integrated Security=true;TrustServerCertificate=true");
        builder.ConfigureTestServices(services =>
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, ConfigureTestBearer));
    }

    #endregion

    #region Nested Types

    private sealed class DependencyGraphFactory : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ConfigureTestingHost(builder);
        }

        #endregion
    }

    private sealed class HardenedApiFactory : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ConfigureTestingHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentUserProvider>();
                services.RemoveAll<IDashboardProvider>();
                services.AddScoped<ICurrentUserProvider, FakeCurrentUserProvider>();
                services.AddScoped<IDashboardProvider, FakeDashboardProvider>();
            });
        }

        #endregion
    }

    private sealed class ProductionSigningKeyFactory : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("AzureAd:Instance", "https://login.microsoftonline.com/");
            builder.UseSetting("AzureAd:TenantId", "beacon-host-tests");
            builder.UseSetting("AzureAd:ClientId", Audience);
            builder.UseSetting("Authentication:Audience", Audience);
            builder.UseSetting("Authentication:Issuer", Issuer);
            builder.UseSetting("Authentication:SigningKey", SigningKey);
            builder.UseSetting("Cors:AllowedOrigins:0", "https://spa.beacon.test");
            builder.UseSetting("ConnectionStrings:DatabaseConnection", "Server=(local);Database=BeaconArHostTests;Integrated Security=true;TrustServerCertificate=true");
        }

        #endregion
    }

    private sealed class ProductionConfigurationFactory(string? missingSetting = null) : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "AzureAd:Instance",
                string.Equals(missingSetting, "AzureAd:Instance", StringComparison.Ordinal) ? " " : "https://login.microsoftonline.com/");
            builder.UseSetting(
                "AzureAd:TenantId",
                string.Equals(missingSetting, "AzureAd:TenantId", StringComparison.Ordinal) ? " " : "beacon-host-tests");
            builder.UseSetting(
                "AzureAd:ClientId",
                string.Equals(missingSetting, "AzureAd:ClientId", StringComparison.Ordinal) ? " " : Audience);
            if (!string.Equals(missingSetting, "Cors:AllowedOrigins", StringComparison.Ordinal))
                builder.UseSetting("Cors:AllowedOrigins:0", "https://spa.beacon.test");
            builder.UseSetting(
                "ConnectionStrings:DatabaseConnection",
                "Server=(local);Database=BeaconArHostTests;Integrated Security=true;TrustServerCertificate=true");
        }

        #endregion
    }

    private sealed class UnexpectedErrorApiFactory : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ConfigureTestingHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICurrentUserProvider>();
                services.RemoveAll<IDashboardProvider>();
                services.AddScoped<ICurrentUserProvider, FakeCurrentUserProvider>();
                services.AddScoped<IDashboardProvider, ThrowingDashboardProvider>();
            });
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
            Task.FromResult(new DashboardSummaryDto(0, 0, 0, 0, 0, DateTimeOffset.UtcNow));

        #endregion
    }

    private sealed class ThrowingDashboardProvider : IDashboardProvider
    {
        #region Public Methods

        public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("sensitive-database-detail");

        #endregion
    }

    #endregion
}
