using System.Text;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Reporting.Contracts;
using BeaconAr.Providers.Access;
using BeaconAr.Providers.Reporting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BeaconAr.WebApi.Tests;

internal sealed class BeaconArSecurityTestHost : WebApplicationFactory<Program>
{
    #region Nested Types

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
            Task.FromResult(new DashboardSummaryDto(1, 2, 3, 4, 5, DateTimeOffset.UtcNow));

        #endregion
    }

    private sealed class FixedHealthCheck(HealthStatus status) : IHealthCheck
    {
        #region Public Methods

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new HealthCheckResult(status));

        #endregion
    }

    #endregion

    #region Constants

    public const string AllowedOrigin = "https://spa.beacon.test";

    #endregion

    #region Fields

    private readonly string _environment;
    private readonly HealthStatus _readiness;
    private readonly bool _useProductionIdentityProvider;

    #endregion

    #region Constructors

    public BeaconArSecurityTestHost(
        string environment = "Testing",
        HealthStatus readiness = HealthStatus.Healthy,
        bool useProductionIdentityProvider = false)
    {
        _environment = environment;
        _readiness = readiness;
        _useProductionIdentityProvider = useProductionIdentityProvider;
    }

    #endregion

    #region Overrides

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.UseSetting("Authentication:Authority", BeaconArTestTokenFactory.Issuer);
        builder.UseSetting("Authentication:Audience", BeaconArTestTokenFactory.Audience);
        builder.UseSetting("Authentication:Issuer", BeaconArTestTokenFactory.Issuer);
        builder.UseSetting("Authentication:Permissions:Read", "business.read");
        builder.UseSetting("Authentication:Permissions:Write", "business.write");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        builder.UseSetting(
            "ConnectionStrings:DatabaseConnection",
            "Server=(local);Database=BeaconArSecurityTests;Integrated Security=true;TrustServerCertificate=true");
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, ConfigureTestBearer);
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration(
                    "self",
                    static _ => new FixedHealthCheck(HealthStatus.Healthy),
                    failureStatus: null,
                    tags: ["live"]));
                options.Registrations.Add(new HealthCheckRegistration(
                    "database",
                    _ => new FixedHealthCheck(_readiness),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready"]));
            });

            services.RemoveAll<IDashboardProvider>();
            services.AddScoped<IDashboardProvider, FakeDashboardProvider>();
            if (!_useProductionIdentityProvider)
            {
                services.RemoveAll<ICurrentUserProvider>();
                services.AddScoped<ICurrentUserProvider, FakeCurrentUserProvider>();
            }
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
        options.TokenValidationParameters.IssuerSigningKey = key;
    }

    #endregion

}
