namespace BeaconAr.WebApi.Endpoints;

internal static class ApiRootEndpoint
{
    #region Public Methods

    public static void MapApiRoot(this IEndpointRouteBuilder endpoints)
    {
        static IResult Response() => Results.Ok(new ApiRootResponse(ApiMetadata.Name, ApiMetadata.ProductVersion));

        endpoints.MapGet("/", Response)
            .WithName("getApiRoot")
            .WithTags("Discovery")
            .Produces<ApiRootResponse>()
            .AllowAnonymous();
        endpoints.MapMethods("/", [HttpMethods.Head], static () => Results.Ok())
            .ExcludeFromDescription()
            .AllowAnonymous();
    }

    #endregion
}
