using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class BeaconArApiTransportMatrixTests
{
    #region Public Methods

    [TestMethod]
    public async Task CorsAllowsOnlyTheConfiguredSpaOriginForActualRequests()
    {
        await using BeaconArSecurityTestHost factory = new();

        using HttpClient knownClient = factory.CreateClient();
        knownClient.DefaultRequestHeaders.Add("Origin", BeaconArSecurityTestHost.AllowedOrigin);
        using HttpResponseMessage known = await knownClient.GetAsync("/api/v1/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, known.StatusCode);
        AssertHeader(known, "Access-Control-Allow-Origin", BeaconArSecurityTestHost.AllowedOrigin);
        Assert.IsFalse(known.Headers.Contains("Access-Control-Allow-Credentials"));

        using HttpClient unknownClient = factory.CreateClient();
        unknownClient.DefaultRequestHeaders.Add("Origin", "https://untrusted.example.test");
        using HttpResponseMessage unknown = await unknownClient.GetAsync("/api/v1/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.IsFalse(unknown.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [TestMethod]
    public async Task CorsPreflightAllowsKnownOriginHeadersAndMethodsButDoesNotEchoUnknownOrigins()
    {
        await using BeaconArSecurityTestHost factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage known = await client.SendAsync(Preflight(BeaconArSecurityTestHost.AllowedOrigin));
        Assert.AreEqual(HttpStatusCode.NoContent, known.StatusCode);
        AssertHeader(known, "Access-Control-Allow-Origin", BeaconArSecurityTestHost.AllowedOrigin);
        AssertHeaderContains(known, "Access-Control-Allow-Methods", "PUT");
        AssertHeaderContains(known, "Access-Control-Allow-Headers", "authorization");
        AssertHeaderContains(known, "Access-Control-Allow-Headers", "if-match");

        using HttpResponseMessage unknown = await client.SendAsync(Preflight("https://untrusted.example.test"));
        Assert.IsFalse(unknown.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.IsFalse(unknown.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [TestMethod]
    public async Task HealthEndpointsAreAnonymousSplitAndDoNotExposeDependencyDetails()
    {
        await using BeaconArSecurityTestHost healthyFactory = new();
        using HttpClient healthyClient = healthyFactory.CreateClient();
        using HttpResponseMessage live = await healthyClient.GetAsync("/alive");
        using HttpResponseMessage ready = await healthyClient.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);
        Assert.AreEqual("Healthy", await live.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, ready.StatusCode);
        Assert.AreEqual("Healthy", await ready.Content.ReadAsStringAsync());

        await using BeaconArSecurityTestHost unhealthyFactory = new(readiness: HealthStatus.Unhealthy);
        using HttpClient unhealthyClient = unhealthyFactory.CreateClient();
        using HttpResponseMessage stillLive = await unhealthyClient.GetAsync("/alive");
        using HttpResponseMessage notReady = await unhealthyClient.GetAsync("/health");
        string body = await notReady.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, stillLive.StatusCode);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, notReady.StatusCode);
        Assert.AreEqual("Unhealthy", body);
        Assert.IsFalse(body.Contains("database", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(body.Contains("Server=", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task ProductionRejectsHttpAndAllowsHttpsBeforeApplyingEndpointAuthorization()
    {
        await using BeaconArSecurityTestHost factory = new(environment: "Production");
        using HttpClient httpClient = factory.CreateClient();
        using HttpResponseMessage http = await httpClient.GetAsync("/alive");
        string body = await http.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.BadRequest, http.StatusCode, body);
        StringAssert.Contains(body, "\"code\":\"https_required\"");

        using HttpClient httpsClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });
        using HttpResponseMessage live = await httpsClient.GetAsync("/alive");
        using HttpResponseMessage api = await httpsClient.GetAsync("/api/v1/me");

        Assert.AreEqual(HttpStatusCode.OK, live.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, api.StatusCode);
    }

    #endregion

    #region Private Methods

    private static void AssertHeader(HttpResponseMessage response, string name, string expected)
    {
        Assert.IsTrue(response.Headers.TryGetValues(name, out IEnumerable<string>? values));
        CollectionAssert.Contains(values.ToArray(), expected);
    }

    private static void AssertHeaderContains(HttpResponseMessage response, string name, string expected)
    {
        Assert.IsTrue(response.Headers.TryGetValues(name, out IEnumerable<string>? values));
        string combined = string.Join(',', values);
        Assert.IsTrue(combined.Split(',').Any(value => string.Equals(value.Trim(), expected, StringComparison.OrdinalIgnoreCase)), combined);
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        HttpRequestMessage request = new(HttpMethod.Options, "/api/v1/products/1");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "PUT");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type,if-match");
        return request;
    }

    #endregion
}
