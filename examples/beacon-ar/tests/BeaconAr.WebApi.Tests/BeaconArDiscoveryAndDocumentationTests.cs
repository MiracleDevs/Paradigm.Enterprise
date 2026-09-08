using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BeaconAr.WebApi.Tests;

[TestClass]
public sealed class BeaconArDiscoveryAndDocumentationTests
{
    #region Public Methods

    [TestMethod]
    public async Task RootIsAnonymousAndReturnsOnlySafeApplicationMetadata()
    {
        await using BeaconArSecurityTestHost factory = new();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonProperty[] properties = document.RootElement.EnumerateObject().ToArray();
        Assert.AreEqual(2, properties.Length);
        Assert.IsTrue(properties.Any(static property => property.NameEquals("name")));
        Assert.IsTrue(properties.Any(static property => property.NameEquals("version")));
        Assert.AreEqual("Beacon AR API", document.RootElement.GetProperty("name").GetString());
        Assert.IsFalse(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("version").GetString()));

        using HttpRequestMessage headRequest = new(HttpMethod.Head, "/");
        using HttpResponseMessage head = await client.SendAsync(headRequest);
        Assert.AreEqual(HttpStatusCode.OK, head.StatusCode);
        Assert.AreEqual(0, (await head.Content.ReadAsByteArrayAsync()).Length);
    }

    [TestMethod]
    public async Task SwaggerJsonAndUiAreAnonymousOnlyInDevelopment()
    {
        await using DevelopmentDocumentationHost development = new();
        using HttpClient developmentClient = development.CreateClient();
        using HttpResponseMessage document = await developmentClient.GetAsync("/openapi/v1.json");
        using HttpResponseMessage ui = await developmentClient.GetAsync("/swagger/index.html");

        Assert.AreEqual(HttpStatusCode.OK, document.StatusCode, await document.Content.ReadAsStringAsync());
        Assert.AreEqual("application/json", document.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual(HttpStatusCode.OK, ui.StatusCode);

        await using BeaconArSecurityTestHost testing = new();
        using HttpClient testingClient = testing.CreateClient();
        testingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            BeaconArTestTokenFactory.Create(scope: "business.read"));
        using HttpResponseMessage unavailable = await testingClient.GetAsync("/openapi/v1.json");
        Assert.AreEqual(HttpStatusCode.NotFound, unavailable.StatusCode);
    }

    #endregion

    #region Nested Types

    private sealed class DevelopmentDocumentationHost : WebApplicationFactory<Program>
    {
        #region Overrides

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(
                "ConnectionStrings:DatabaseConnection",
                "Server=(local);Database=BeaconArDocumentationTests;Integrated Security=true;TrustServerCertificate=true");
        }

        #endregion
    }

    #endregion
}
