using Paradigm.Web.ApiTemplate.Data.Sample.Context;
using Paradigm.Web.ApiTemplate.HealthChecks;
using Paradigm.Web.ApiTemplate.WebApi.Exceptions.Handlers.Resources;
using Paradigm.Web.ApiTemplate.WebApi.Extensions;
using Paradigm.Web.ApiTemplate.WebApi.JsonSerializerContexts;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
//using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Paradigm.Enterprise.Data.SqlServer.Context;
using Paradigm.Enterprise.Data.SqlServer.Extensions;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Providers.Utils;
using Paradigm.Enterprise.WebApi.Exceptions.Handlers;
using Paradigm.Enterprise.WebApi.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationInsightsTelemetry();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddApplicationInsights();

var configuration = builder.Configuration;

const string title = "Paradigm.Web.ApiTemplate API";

//builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "Authentication:Microsoft");

builder.Services.AddAuthorization();

builder.Services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(static o =>
    {
        // TODO: add auto-generated JsonContexts here
        o.JsonSerializerOptions.TypeInfoResolverChain.Add(ApiModelsJsonContext.Default);
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks().AddDbContextCheck<SampleDbContext>("Sql Server");
builder.Services.AddSwaggerGen(opt =>
{
	opt.SwaggerDoc("v1", new OpenApiInfo
	{
		Version = "v1",
		Title = title,
		Description = "[API description]",
		Contact = new OpenApiContact
		{
			Name = "[API name]",
			Email = string.Empty,
			Url = new Uri(configuration.GetValue<string>("WebClientBaseUrl") ?? "http://localhost:4200"),
		},
	});
	opt.AddSecurityDefinition("AuthorizationToken", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please enter a GUID token",
		Name = Paradigm.Web.ApiTemplate.WebApi.Middlewares.AuthenticationMiddleware.AuthenticationHeader,
		Type = SecuritySchemeType.ApiKey,
		BearerFormat = "JWT"
	});

	opt.AddSecurityRequirement(document => new OpenApiSecurityRequirement
	{
		[new OpenApiSecuritySchemeReference("AuthorizationToken", document)] = new List<string>()
	});

	opt.CustomOperationIds(operationIdSelector =>
	{
		var routeValues = operationIdSelector.ActionDescriptor.RouteValues;
		return $"{routeValues["controller"]}{routeValues["action"]}";
	});
});

const string corsPolicy = "CorsPolicy";
builder.Services.AddCors(o => o.AddPolicy(corsPolicy,
    b => b.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

const string dbConnectionStringName = "SampleConnection";

builder.Services
    .AddScoped<SqlServerDbContextConnectionProvider>()
    .RegisterContext<SampleDbContext>(dbConnectionStringName);

builder.Services
    .AddScoped<IUnitOfWork, UnitOfWork>()
    .AddScoped<IExceptionHandler, ExceptionHandler>(_ => new ExceptionHandler(typeof(Exceptions)))
    .AddTransient<AsyncProcessManager>()
    .RegisterLoggedUserService()
    .RegisterRepositories()
    .RegisterProviders()
    .RegisterServices([])
    .RegisterMappers()
    .RegisterEntities()
    .RegisterDtos();

var app = builder.Build();
var env = app.Services.GetRequiredService<IWebHostEnvironment>();

if (env.IsDevelopment())
{
    app.UseSwagger();
    app.MapSwagger("/openapi/{documentName}.json");
    app.MapScalarApiReference();
}

app.UseCors(corsPolicy);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseOwnExceptionHandler();
app.UseOwnAuthentication();
app.MapHealthChecks("/health", new HealthCheckOptions
{
	ResultStatusCodes =
	{
		[HealthStatus.Healthy] = StatusCodes.Status200OK,
		[HealthStatus.Degraded] = StatusCodes.Status200OK,
		[HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
	},
	ResponseWriter = async (context, report) =>
	{
		context.Response.ContentType = "text/html";
		await context.Response.WriteAsync(HealthCheckCustomResponse.GetResult(title, report));
	}
});
app.MapControllers();
app.Run();
