using Paradigm.Enterprise.WebApi.Middlewares;
using System.Text.Json.Serialization;

namespace Paradigm.Enterprise.WebApi.JsonSerializerContexts;

[JsonSerializable(typeof(ExceptionHandlerMiddleware.Error))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ValidationProblemDetails))]
internal partial class ApiModelsJsonContext : JsonSerializerContext
{
}