using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeaconAr.Data.Access;
using BeaconAr.Data.MasterData;
using BeaconAr.Data.Operations;
using BeaconAr.Data.Receivables.Context;
using BeaconAr.Data.Sales;
using BeaconAr.Domain.MasterData.Application;
using BeaconAr.Domain.Operations;
using BeaconAr.Domain.Sales.Application;
using BeaconAr.Providers.Access;
using BeaconAr.Providers.MasterData;
using BeaconAr.Providers.Sales;
using BeaconAr.WebApi;
using BeaconAr.WebApi.Access;
using BeaconAr.WebApi.Http;
using BeaconAr.WebApi.OpenApi;
using BeaconAr.WebApi.Security;
using BeaconAr.WebApi.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Paradigm.Enterprise.Data.SqlServer.Context;
using Paradigm.Enterprise.Data.SqlServer.Extensions;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.WebApi.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ApiContract.MaxJsonRequestBodySize);

string? authority = builder.Configuration["Authentication:Authority"];
string? audience = builder.Configuration["Authentication:Audience"];
string? issuer = builder.Configuration["Authentication:Issuer"];
string? configuredSigningKey = builder.Configuration["Authentication:SigningKey"];
string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
bool isLocal = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
bool isOpenApiGeneration =
    string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, "GetDocument.Insider", StringComparison.OrdinalIgnoreCase) ||
    AppDomain.CurrentDomain.GetAssemblies().Any(static assembly => assembly.GetName().Name?.Contains("ApiDescription", StringComparison.OrdinalIgnoreCase) == true) ||
    Environment.GetCommandLineArgs().Any(static argument => argument.Contains("dotnet-getdocument", StringComparison.OrdinalIgnoreCase));
if (!isLocal && !isOpenApiGeneration && (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(issuer) || origins.Length == 0))
    throw new InvalidOperationException("Authentication authority, audience, issuer, and at least one CORS origin are required outside local environments.");
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
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, BeaconArApiJsonContext.Default);
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
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, BeaconArApiJsonContext.Default);
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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.SaveToken = false;
    options.RequireHttpsMetadata = !isLocal;
    options.Authority = authority;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromMinutes(2),
        NameClaimType = "name",
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            await AuthenticationProblemWriter.WriteAsync(context.HttpContext, 401, "unauthorized", "Unauthorized", "A valid bearer access token is required.");
        },
        OnForbidden = context => AuthenticationProblemWriter.WriteAsync(context.HttpContext, 403, "forbidden", "Forbidden", "The authenticated user is not authorized for this operation."),
    };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy(BeaconPolicies.Read, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.Resource is HttpContext http && http.RequestServices.GetRequiredService<PermissionEvaluator>().HasPolicy(context.User, BeaconPolicies.Read)));
    options.AddPolicy(BeaconPolicies.Write, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
        context.Resource is HttpContext http && http.RequestServices.GetRequiredService<PermissionEvaluator>().HasPolicy(context.User, BeaconPolicies.Write)));
});
builder.Services.AddCors(options => options.AddPolicy("beacon-spa", policy => policy
    .WithOrigins(origins.Length == 0 ? ["https://localhost:4200"] : origins)
    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
    .WithHeaders("Authorization", "Content-Type", "If-Match", "Idempotency-Key", "traceparent", "tracestate", "baggage")
    .WithExposedHeaders("ETag", "Location", "Idempotency-Replayed")));
builder.Services.AddOpenApi("v1", options => options.AddDocumentTransformer<BearerSecurityDocumentTransformer>());

string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection")
    ?? (isOpenApiGeneration ? "Server=(local);Database=BeaconAr;Integrated Security=true;TrustServerCertificate=true" : throw new InvalidOperationException("DatabaseConnection is required."));
builder.Services.AddHealthChecks().AddSqlServer(connectionString, name: "database", timeout: TimeSpan.FromSeconds(3), tags: ["ready"]);
builder.Services.AddScoped<SqlServerDbContextConnectionProvider>();
builder.Services.RegisterContext<ReceivablesDbContext>("DatabaseConnection");
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.RegisterLoggedUserService();
builder.Services.RegisterRepositories(typeof(ApplicationUserRepository).Assembly);
builder.Services.RegisterProviders(typeof(ProductProvider).Assembly);
Assembly domainAssembly = typeof(BeaconAr.Domain.Receivables.Entities.Product).Assembly;
builder.Services.RegisterMappers(domainAssembly);
builder.Services.RegisterEntities(domainAssembly);
builder.Services.AddScoped<IMasterDataPersistenceErrorClassifier, MasterDataPersistenceErrorClassifier>();
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
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestBodyLimitMiddleware>();
app.UseMiddleware<CurrentUserMiddleware>();
app.MapDefaultEndpoints();
app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
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
