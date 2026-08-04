using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeaconAr.Data.Access.Repositories;
using BeaconAr.Data.Access.Context;
using BeaconAr.Data.MasterData;
using BeaconAr.Data.Operations;
using BeaconAr.Data.Sales;
using BeaconAr.Data.MasterData.Context;
using BeaconAr.Data.Operations.Context;
using BeaconAr.Data.Sales.Context;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Providers.Access;
using BeaconAr.Providers.MasterData;
using BeaconAr.Providers.Sales;
using BeaconAr.WebApi;
using BeaconAr.WebApi.Access;
using BeaconAr.WebApi.Endpoints;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.OpenApi;
using BeaconAr.WebApi.Security;
using BeaconAr.WebApi.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Paradigm.Enterprise.Data.SqlServer.Context;
using Paradigm.Enterprise.Data.SqlServer.Extensions;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.WebApi.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiContract.MaxJsonRequestBodySize);

string? identityInstance = builder.Configuration["AzureAd:Instance"];
string? tenantId = builder.Configuration["AzureAd:TenantId"];
string? clientId = builder.Configuration["AzureAd:ClientId"];
string? configuredSigningKey = builder.Configuration["Authentication:SigningKey"];
string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
bool isLocal = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
if (!isLocal && (string.IsNullOrWhiteSpace(identityInstance) || string.IsNullOrWhiteSpace(tenantId) ||
                 string.IsNullOrWhiteSpace(clientId) || origins.Length == 0 || origins.Any(string.IsNullOrWhiteSpace)))
    throw new InvalidOperationException("AzureAd instance, tenant ID, client ID, and at least one CORS origin are required outside local environments.");
if (!string.IsNullOrWhiteSpace(configuredSigningKey) && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Authentication:SigningKey is permitted only in the isolated Testing environment.");

IdentityModelEventSource.ShowPII = false;
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions.TryAdd("code", ApiContract.StatusCode(context.HttpContext.Response.StatusCode));
    context.ProblemDetails.Extensions.TryAdd("correlationId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.MasterData.Contracts.SortDirection>());
        options.JsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.MasterData.Contracts.AddressUsage>());
        options.JsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.Sales.QuoteStatus>());
        options.JsonSerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.Sales.SalesOrderStatus>());
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, BeaconArApiJsonContract.CreateResolver());
    });
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.MasterData.Contracts.SortDirection>());
    options.SerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.MasterData.Contracts.AddressUsage>());
    options.SerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.Sales.QuoteStatus>());
    options.SerializerOptions.Converters.Add(new CamelCaseStringEnumConverter<BeaconAr.Domain.Sales.SalesOrderStatus>());
    options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetJsonConverter());
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, BeaconArApiJsonContract.CreateResolver());
});
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = context =>
    {
        bool malformed = context.ModelState.Any(static item =>
            item.Value?.Errors.Any(static error => error.Exception is not null) == true ||
            item.Key.StartsWith('$'));
        Dictionary<string, string[]> errors = context.ModelState
            .Where(static item => item.Value?.Errors.Count > 0)
            .ToDictionary(
                static item => ToCamelPath(item.Key),
                item => item.Value!.Errors.Select(error => malformed || error.Exception is not null ? "The JSON body is malformed." : string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The value is invalid." : error.ErrorMessage).ToArray(),
                StringComparer.Ordinal);
        ValidationProblemDetails problem = new(errors)
        {
            Status = 400,
            Title = malformed ? "Bad request" : "One or more validation errors occurred.",
            Detail = malformed ? "The request body could not be read." : "The request could not be processed.",
            Instance = context.HttpContext.Request.Path,
        };
        problem.Extensions["code"] = malformed ? "malformed_request" : "validation_failed";
        problem.Extensions["correlationId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem) { ContentTypes = { ApiContract.ProblemJson } };
    };
});

builder.Services.AddSingleton<PermissionEvaluator>();
builder.Services.AddSingleton<IAuthorizationHandler, DelegatedUserAuthorizationHandler>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.MapInboundClaims = false;
    options.SaveToken = false;
    options.RequireHttpsMetadata = !isLocal;
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(2);
    options.Events ??= new JwtBearerEvents();
    options.Events.OnChallenge = async context =>
    {
        context.HandleResponse();
        await AuthenticationProblemWriter.WriteAsync(context.HttpContext, 401, "unauthorized", "Unauthorized", "A valid bearer access token is required.");
    };
    options.Events.OnForbidden = context => AuthenticationProblemWriter.WriteAsync(
        context.HttpContext,
        403,
        "forbidden",
        "Forbidden",
        "The authenticated user is not authorized for this operation.");

    if (!string.IsNullOrWhiteSpace(configuredSigningKey))
    {
        string testingIssuer = builder.Configuration["Authentication:Issuer"] ??
            throw new InvalidOperationException("Authentication:Issuer is required with the Testing signing key.");
        string testingAudience = builder.Configuration["Authentication:Audience"] ??
            throw new InvalidOperationException("Authentication:Audience is required with the Testing signing key.");
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuredSigningKey));
        options.Authority = null;
        options.Configuration = new OpenIdConnectConfiguration { Issuer = testingIssuer };
        options.Configuration.SigningKeys.Add(key);
        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
        options.TokenValidationParameters.IssuerSigningKey = key;
        options.TokenValidationParameters.ValidIssuer = testingIssuer;
        options.TokenValidationParameters.ValidAudience = testingAudience;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.RequireExpirationTime = true;
        options.TokenValidationParameters.RequireSignedTokens = true;
    }
});
builder.Services.AddAuthorization(options =>
{
    AuthorizationPolicy delegatedUserPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(DelegatedUserRequirement.Instance)
        .Build();
    options.DefaultPolicy = delegatedUserPolicy;
    options.FallbackPolicy = delegatedUserPolicy;
    options.AddPolicy(BeaconPolicies.Read, policy => policy.RequireAuthenticatedUser()
        .AddRequirements(DelegatedUserRequirement.Instance).RequireAssertion(context =>
        context.Resource is HttpContext http && http.RequestServices.GetRequiredService<PermissionEvaluator>().HasPolicy(context.User, BeaconPolicies.Read)));
    options.AddPolicy(BeaconPolicies.Write, policy => policy.RequireAuthenticatedUser()
        .AddRequirements(DelegatedUserRequirement.Instance).RequireAssertion(context =>
        context.Resource is HttpContext http && http.RequestServices.GetRequiredService<PermissionEvaluator>().HasPolicy(context.User, BeaconPolicies.Write)));
});
builder.Services.AddCors(options => options.AddPolicy("beacon-spa", policy => policy
    .WithOrigins(origins.Length == 0 ? ["https://localhost:4200"] : origins)
    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
    .WithHeaders("Authorization", "Content-Type", "If-Match", "Idempotency-Key", "traceparent", "tracestate", "baggage")
    .WithExposedHeaders("ETag", "Location", "Idempotency-Replayed")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(ApiMetadata.OpenApiVersion, new OpenApiInfo
    {
        Title = ApiMetadata.Name,
        Version = ApiMetadata.OpenApiVersion,
    });
    options.DocumentFilter<BeaconArDocumentFilter>();
});

string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection")
    ?? throw new InvalidOperationException("DatabaseConnection is required.");
builder.Services.AddHealthChecks().AddSqlServer(connectionString, name: "database", timeout: TimeSpan.FromSeconds(3), tags: ["ready"]);
builder.Services.AddScoped<SqlServerDbContextConnectionProvider>();
builder.Services.RegisterContext<AccessDbContext>("DatabaseConnection");
builder.Services.RegisterContext<MasterDataDbContext>("DatabaseConnection");
builder.Services.RegisterContext<OperationsDbContext>("DatabaseConnection");
builder.Services.RegisterContext<SalesDbContext>("DatabaseConnection");
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.RegisterLoggedUserService();
builder.Services.RegisterRepositories(typeof(ApplicationUserRepository).Assembly);
builder.Services.RegisterProviders(typeof(ProductProvider).Assembly);
Assembly domainAssembly = typeof(BeaconAr.Domain.MasterData.Entities.Product).Assembly;
builder.Services.RegisterMappers(domainAssembly);
builder.Services.RegisterEntities(domainAssembly);
builder.Services.AddScoped<IMasterDataPersistenceErrorClassifier, MasterDataPersistenceErrorClassifier>();
builder.Services.AddScoped<IIdempotencyPersistenceErrorClassifier, IdempotencyPersistenceErrorClassifier>();
builder.Services.AddScoped<MasterDataMutationCoordinator>();
builder.Services.AddScoped<SalesWorkflowCoordinator>();
builder.Services.AddScoped<ISalesPersistenceErrorClassifier, SalesPersistenceErrorClassifier>();
builder.Services.AddScoped<IPersistenceSession, PersistenceSession>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ApplicationOperationContext>();
builder.Services.AddScoped<IApplicationOperationContext>(static provider => provider.GetRequiredService<ApplicationOperationContext>());
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<CreationIdempotencyService>();

WebApplication app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    HttpContext context = statusCodeContext.HttpContext;
    if (!context.Request.Path.StartsWithSegments("/api"))
        return;

    (string title, string detail) = context.Response.StatusCode switch
    {
        StatusCodes.Status404NotFound => ("Not found", "The requested API route does not exist."),
        StatusCodes.Status405MethodNotAllowed => ("Method not allowed", "The HTTP method is not supported for this API route."),
        StatusCodes.Status413PayloadTooLarge => ("Request too large", "The request body exceeds the configured limit."),
        StatusCodes.Status415UnsupportedMediaType => ("Unsupported media type", "Request bodies must use application/json."),
        _ => ("Request failed", "The request could not be processed."),
    };
    await AuthenticationProblemWriter.WriteAsync(
        context,
        context.Response.StatusCode,
        ApiContract.StatusCode(context.Response.StatusCode),
        title,
        detail);
});
if (!isLocal)
    app.UseMiddleware<HttpsRequirementMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles();
    app.UseSwagger(options => options.RouteTemplate = "openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = $"{ApiMetadata.Name} documentation";
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint($"/openapi/{ApiMetadata.OpenApiVersion}.json", $"{ApiMetadata.Name} {ApiMetadata.OpenApiVersion}");
        options.InjectStylesheet("/swagger/style.css");
    });
}
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestBodyLimitMiddleware>();
app.UseMiddleware<CurrentUserMiddleware>();
app.MapDefaultEndpoints();
app.MapApiRoot();
app.MapControllers().RequireCors("beacon-spa");
app.Run();

static string ToCamelPath(string path)
{
    if (string.IsNullOrEmpty(path))
        return path;
    string[] segments = path.Replace('[', '.').Replace("]", string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries);
    return string.Join('.', segments.Select(static segment => char.IsLetter(segment[0]) ? char.ToLowerInvariant(segment[0]) + segment[1..] : segment));
}

public partial class Program;
