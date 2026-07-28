# Code generation

`Paradigm.Enterprise.CodeGenerator` is a standalone executable project for System.Text.Json contexts, stored-procedure mappers, and an Angular TypeScript client. It is not packaged as a .NET tool. Run it from a source checkout or publish it through the team's release process.

```powershell
dotnet run `
  --project src/Paradigm.Enterprise.CodeGenerator `
  -- --help
```

## Current limitations

Do not treat the JSON-context and stored-procedure modes as working generation paths in the current release. Each mode uses its assembly argument in two incompatible ways: it passes the value to `Assembly.LoadFrom`, which requires a DLL path, and also appends an output directory to the same value. A DLL path cannot also be an output directory. Both generators catch and log the resulting exception, so the process may continue without producing the expected files.

The executable does not expose a switch for running only one generator. Until assembly input and source output are represented by separate options, keep existing generated JSON contexts and mappers under source control and maintain them through the application-local generation process. Verify their contents during review instead of assuming a successful process exit means every generator ran.

The stored-procedure generator is also SQL Server-specific. It emits SQL Server parameter mappers and registration through `SqlParameterMapperFactory`. PostgreSQL applications must implement `INpgsqlParameterMapper` and register each parameters type with `NpgsqlParameterMapperFactory` until PostgreSQL generation is implemented.

## JSON contexts

Source-generated JSON metadata must cover every request, response, nested generic argument, and framework DTO that the host serializes. Add generated contexts to MVC's `TypeInfoResolverChain` and regenerate or update them whenever a provider or controller signature changes.

Review the resulting `[JsonSerializable]` declarations before disabling reflection-based metadata. Missing metadata is usually discovered only when the affected endpoint serializes.

## Stored-procedure mappers

Stored-procedure parameter and result types must be available in a successfully built Data assembly. A mapper implementation translates parameters into provider commands or reads a result row into the declared CLR type. Register each mapper before the first procedure execution.

For SQL Server, use `ISqlParameterMapper` with `SqlParameterMapperFactory`. For PostgreSQL, use `INpgsqlParameterMapper` with `NpgsqlParameterMapperFactory`. Data-reader mapping is shared through `IDataReaderMapper` and `DataReaderMapperFactory`.

## OpenAPI client

The OpenAPI client generator can still run after the two earlier generators log their failures because those exceptions are swallowed. This is not a clean standalone mode, so inspect the log and output file rather than relying on the exit code alone.

Before invoking it, build the application assemblies, start the intended API profile, and confirm its OpenAPI document contains only the expected endpoints. The configured output directory must already contain the `base-client.ts` extension file expected by the bundled settings.

```powershell
dotnet run `
  --project src/Paradigm.Enterprise.CodeGenerator `
  -- `
  --ProjectName Sample `
  --ProvidersAssemblyPath C:/work/Sample.Providers/bin/Debug/net10.0/Sample.Providers.dll `
  --DataAssemblyPath C:/work/Sample.Data/bin/Debug/net10.0/Sample.Data.dll `
  --ProxiesOutput C:/work/client/src/generated `
  --SwaggerUrl https://localhost:7001/swagger/v1/swagger.json
```

Use actual DLL paths for the assembly arguments. Expect the JSON and stored-procedure steps to report their current path errors before client generation runs. Generate only from a trusted OpenAPI endpoint, then review the resulting client diff.

The Visual Studio template also contains an application-local generator scaffold. Keep that scaffold aligned with the application's generated files, and treat the current library source as the authority for runtime contracts.
