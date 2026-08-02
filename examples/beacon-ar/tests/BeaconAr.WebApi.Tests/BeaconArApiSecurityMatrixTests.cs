using System.Net;
using System.Net.Http.Headers;
using BeaconAr.WebApi;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class BeaconArApiSecurityMatrixTests
{
    #region Nested Types

    private sealed record Operation(HttpMethod Method, string Route, string RequestTarget, RequiredPolicy Policy)
    {
        public string Description => $"{Method} {Route}";
    }

    private sealed record PermissionCase(string Name, string Token, bool CanRead, bool CanWrite);

    private enum RequiredPolicy
    {
        Authenticated,
        Read,
        Write,
    }

    #endregion

    #region Properties

    private static IReadOnlyList<Operation> Operations { get; } =
    [
        new(HttpMethod.Get, "api/v1/me", "/api/v1/me", RequiredPolicy.Authenticated),
        new(HttpMethod.Get, "api/v1/dashboard/summary", "/api/v1/dashboard/summary", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/products", "/api/v1/products?sortDirection=invalid", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/products/{id:int}", "/api/v1/products/0", RequiredPolicy.Read),
        new(HttpMethod.Post, "api/v1/products", "/api/v1/products", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/products/{id:int}", "/api/v1/products/0", RequiredPolicy.Write),
        new(HttpMethod.Delete, "api/v1/products/{id:int}", "/api/v1/products/0", RequiredPolicy.Write),
        new(HttpMethod.Get, "api/v1/customers", "/api/v1/customers?sortDirection=invalid", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/customers/{id:int}", "/api/v1/customers/0", RequiredPolicy.Read),
        new(HttpMethod.Post, "api/v1/customers", "/api/v1/customers", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/customers/{id:int}", "/api/v1/customers/0", RequiredPolicy.Write),
        new(HttpMethod.Delete, "api/v1/customers/{id:int}", "/api/v1/customers/0", RequiredPolicy.Write),
        new(HttpMethod.Get, "api/v1/addresses", "/api/v1/addresses?sortDirection=invalid", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/addresses/{id:int}", "/api/v1/addresses/0", RequiredPolicy.Read),
        new(HttpMethod.Post, "api/v1/addresses", "/api/v1/addresses", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/addresses/{id:int}", "/api/v1/addresses/0", RequiredPolicy.Write),
        new(HttpMethod.Delete, "api/v1/addresses/{id:int}", "/api/v1/addresses/0", RequiredPolicy.Write),
        new(HttpMethod.Get, "api/v1/carriers", "/api/v1/carriers?sortDirection=invalid", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/carriers/{id:int}", "/api/v1/carriers/0", RequiredPolicy.Read),
        new(HttpMethod.Post, "api/v1/carriers", "/api/v1/carriers", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/carriers/{id:int}", "/api/v1/carriers/0", RequiredPolicy.Write),
        new(HttpMethod.Delete, "api/v1/carriers/{id:int}", "/api/v1/carriers/0", RequiredPolicy.Write),
        new(HttpMethod.Get, "api/v1/quotes", "/api/v1/quotes?sortDirection=invalid", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/quotes/{id:int}", "/api/v1/quotes/0", RequiredPolicy.Read),
        new(HttpMethod.Post, "api/v1/quotes", "/api/v1/quotes", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/quotes/{id:int}", "/api/v1/quotes/0", RequiredPolicy.Write),
        new(HttpMethod.Delete, "api/v1/quotes/{id:int}", "/api/v1/quotes/0", RequiredPolicy.Write),
        new(HttpMethod.Post, "api/v1/quotes/{id:int}/status-transitions", "/api/v1/quotes/0/status-transitions", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/quotes/{id:int}/sales-order", "/api/v1/quotes/0/sales-order", RequiredPolicy.Write),
        new(HttpMethod.Get, "api/v1/sales-orders", "/api/v1/sales-orders?sortDirection=invalid", RequiredPolicy.Read),
        new(HttpMethod.Get, "api/v1/sales-orders/{id:int}", "/api/v1/sales-orders/0", RequiredPolicy.Read),
        new(HttpMethod.Post, "api/v1/sales-orders", "/api/v1/sales-orders", RequiredPolicy.Write),
        new(HttpMethod.Put, "api/v1/sales-orders/{id:int}", "/api/v1/sales-orders/0", RequiredPolicy.Write),
        new(HttpMethod.Delete, "api/v1/sales-orders/{id:int}", "/api/v1/sales-orders/0", RequiredPolicy.Write),
        new(HttpMethod.Post, "api/v1/sales-orders/{id:int}/status-transitions", "/api/v1/sales-orders/0/status-transitions", RequiredPolicy.Write),
    ];

    #endregion

    #region Public Methods

    [TestMethod]
    public void HostedEndpointCatalogHasTheExactAuthorizationPolicyForAll35Operations()
    {
        using BeaconArSecurityTestHost factory = new();
        EndpointDataSource endpoints = factory.Services.GetRequiredService<EndpointDataSource>();
        RouteEndpoint[] controllerEndpoints = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Where(static endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .ToArray();

        Assert.AreEqual(35, controllerEndpoints.Length);
        foreach (Operation operation in Operations)
        {
            RouteEndpoint endpoint = controllerEndpoints.Single(candidate =>
                string.Equals(candidate.RoutePattern.RawText, operation.Route, StringComparison.Ordinal) &&
                candidate.Metadata.GetRequiredMetadata<IHttpMethodMetadata>().HttpMethods.Contains(operation.Method.Method, StringComparer.Ordinal));
            IReadOnlyList<IAuthorizeData> authorization = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            Assert.IsNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>(), operation.Description);
            Assert.IsTrue(authorization.Count > 0, operation.Description);

            string[] policies = authorization
                .Select(static item => item.Policy)
                .Where(static policy => !string.IsNullOrWhiteSpace(policy))
                .Cast<string>()
                .ToArray();
            if (operation.Policy == RequiredPolicy.Authenticated)
                Assert.AreEqual(0, policies.Length, operation.Description);
            else
                CollectionAssert.Contains(policies, BeaconPolicies.Read, operation.Description);
            if (operation.Policy == RequiredPolicy.Write)
                CollectionAssert.Contains(policies, BeaconPolicies.Write, operation.Description);
            else
                CollectionAssert.DoesNotContain(policies, BeaconPolicies.Write, operation.Description);
        }
    }

    [TestMethod]
    public async Task EveryOperationChallengesRequestsWithoutABearerToken()
    {
        await using BeaconArSecurityTestHost factory = new();
        using HttpClient client = factory.CreateClient();

        foreach (Operation operation in Operations)
        {
            using HttpResponseMessage response = await SendAsync(client, operation);
            await AssertProblem(response, HttpStatusCode.Unauthorized, "unauthorized", operation.Description);
        }
    }

    [TestMethod]
    public async Task ReadOnlyAndWriteTokensEnforceTheHostedOperationMatrix()
    {
        await using BeaconArSecurityTestHost factory = new();
        using HttpClient readClient = AuthorizedClient(factory, BeaconArTestTokenFactory.Create(scope: "business.read"));
        using HttpClient writeClient = AuthorizedClient(factory, BeaconArTestTokenFactory.Create(scope: "business.write"));

        foreach (Operation operation in Operations)
        {
            using HttpResponseMessage readResponse = await SendAsync(readClient, operation);
            if (operation.Policy == RequiredPolicy.Write)
                await AssertProblem(readResponse, HttpStatusCode.Forbidden, "forbidden", operation.Description);
            else
                AssertIsNotAuthenticationFailure(readResponse, operation.Description);

            using HttpResponseMessage writeResponse = await SendAsync(writeClient, operation);
            AssertIsNotAuthenticationFailure(writeResponse, operation.Description);
        }
    }

    [TestMethod]
    public async Task InvalidExpiredFutureAndMisdirectedTokensAreUnauthorized()
    {
        DateTime now = DateTime.UtcNow;
        (string Name, string Token)[] cases =
        [
            ("malformed", "not-a-jwt"),
            ("invalid signature", BeaconArTestTokenFactory.Create(signingKey: BeaconArTestTokenFactory.SigningKey + "-wrong")),
            ("expired", BeaconArTestTokenFactory.Create(notBefore: now.AddMinutes(-20), expires: now.AddMinutes(-10))),
            ("future", BeaconArTestTokenFactory.Create(notBefore: now.AddMinutes(10), expires: now.AddMinutes(20))),
            ("wrong issuer", BeaconArTestTokenFactory.Create(issuer: "https://wrong-issuer.example.test")),
            ("wrong audience", BeaconArTestTokenFactory.Create(audience: "api://wrong-audience")),
            ("missing expiration", BeaconArTestTokenFactory.Create(includeExpiration: false)),
            ("unsigned", BeaconArTestTokenFactory.CreateUnsigned()),
        ];

        await using BeaconArSecurityTestHost factory = new();
        foreach ((string name, string token) in cases)
        {
            using HttpClient client = AuthorizedClient(factory, token);
            using HttpResponseMessage response = await client.GetAsync("/api/v1/me");
            await AssertProblem(response, HttpStatusCode.Unauthorized, "unauthorized", name);
        }
    }

    [TestMethod]
    public async Task ScopeAndRoleEvaluationIsExactCaseSensitiveAndWriteImpliesRead()
    {
        PermissionCase[] cases =
        [
            new("no grants", BeaconArTestTokenFactory.Create(), false, false),
            new("read scope", BeaconArTestTokenFactory.Create(scope: "business.read"), true, false),
            new("write scope", BeaconArTestTokenFactory.Create(scope: "business.write"), true, true),
            new("space-delimited scope", BeaconArTestTokenFactory.Create(scope: "unrelated business.read another"), true, false),
            new("read role", BeaconArTestTokenFactory.Create(roles: ["business.read"]), true, false),
            new("write role", BeaconArTestTokenFactory.Create(roles: ["business.write"]), true, true),
            new("roles array", BeaconArTestTokenFactory.Create(roles: ["unrelated", "business.read"]), true, false),
            new("singular role claim", BeaconArTestTokenFactory.Create(roles: ["business.read"], roleClaimType: "role"), false, false),
            new("scope case variant", BeaconArTestTokenFactory.Create(scope: "Business.Read"), false, false),
            new("role case variant", BeaconArTestTokenFactory.Create(roles: ["BUSINESS.WRITE"]), false, false),
            new("scope suffix", BeaconArTestTokenFactory.Create(scope: "business.read.extra"), false, false),
            new("scope prefix", BeaconArTestTokenFactory.Create(scope: "prefix-business.read"), false, false),
            new("comma-delimited scope", BeaconArTestTokenFactory.Create(scope: "business.read,business.write"), false, false),
        ];

        await using BeaconArSecurityTestHost factory = new();
        foreach (PermissionCase permission in cases)
        {
            using HttpClient client = AuthorizedClient(factory, permission.Token);
            using HttpResponseMessage read = await client.GetAsync("/api/v1/dashboard/summary");
            Assert.AreEqual(permission.CanRead ? HttpStatusCode.OK : HttpStatusCode.Forbidden, read.StatusCode, permission.Name);

            using HttpResponseMessage write = await client.PostAsync("/api/v1/products", content: null);
            if (permission.CanWrite)
                AssertIsNotAuthenticationFailure(write, permission.Name);
            else
                Assert.AreEqual(HttpStatusCode.Forbidden, write.StatusCode, permission.Name);
        }
    }

    [TestMethod]
    public async Task AuthenticatedIdentitiesMissingRequiredClaimsAreForbiddenBeforeDataAccess()
    {
        (string Name, string Token)[] cases =
        [
            ("subject", BeaconArTestTokenFactory.Create(scope: "business.read", includeSubject: false)),
            ("display name", BeaconArTestTokenFactory.Create(scope: "business.read", includeName: false)),
        ];

        await using BeaconArSecurityTestHost factory = new(useProductionIdentityProvider: true);
        foreach ((string name, string token) in cases)
        {
            using HttpClient client = AuthorizedClient(factory, token);
            using HttpResponseMessage response = await client.GetAsync("/api/v1/me");
            await AssertProblem(response, HttpStatusCode.Forbidden, "forbidden", name);
            StringAssert.Contains(await response.Content.ReadAsStringAsync(), "authenticated identity is incomplete", StringComparison.OrdinalIgnoreCase);
        }
    }

    #endregion

    #region Private Methods

    private static HttpClient AuthorizedClient(BeaconArSecurityTestHost factory, string token)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static void AssertIsNotAuthenticationFailure(HttpResponseMessage response, string message)
    {
        Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode, message);
        Assert.AreNotEqual(HttpStatusCode.Forbidden, response.StatusCode, message);
    }

    private static async Task AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode status,
        string code,
        string message)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(status, response.StatusCode, $"{message}: {body}");
        Assert.AreEqual(ApiContract.ProblemJson, response.Content.Headers.ContentType?.MediaType, message);
        StringAssert.Contains(body, $"\"code\":\"{code}\"", message);
        StringAssert.Contains(body, "\"correlationId\":", message);
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, Operation operation)
    {
        using HttpRequestMessage request = new(operation.Method, operation.RequestTarget);
        return await client.SendAsync(request);
    }

    #endregion

}
