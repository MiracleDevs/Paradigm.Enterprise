using System.Text.Json.Serialization;

namespace Paradigm.Web.ApiTemplate.WebApi.JsonSerializerContexts;

[JsonSerializable(typeof(Paradigm.Enterprise.WebApi.Middlewares.ExceptionHandlerMiddleware.Error))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
internal partial class ApiModelsJsonContext : JsonSerializerContext
{
}