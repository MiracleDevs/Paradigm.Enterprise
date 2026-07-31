# Code generation

Code generation is part of the installable `Paradigm.Enterprise.Cli`; clients do not build or install a separate `Paradigm.Enterprise.CodeGenerator` executable. Install the single tool through a local manifest, then invoke the required mode independently:

```powershell
dotnet tool run paradigm generate json `
  --project-name Sample `
  --assembly C:/work/Sample.Providers/bin/Debug/net10.0/Sample.Providers.dll `
  --output C:/work/Sample.Providers

dotnet tool run paradigm generate mappers `
  --project-name Sample `
  --assembly C:/work/Sample.Data/bin/Debug/net10.0/Sample.Data.dll `
  --output C:/work/Sample.Data

dotnet tool run paradigm generate client `
  --document https://localhost:7001/swagger/v1/swagger.json `
  --output C:/work/client/src/generated
```

Assembly input and source output are separate options. This fixes the former path ambiguity where one value was treated as both a DLL and a directory. A failed mode returns a nonzero result instead of logging an error and continuing as though generation succeeded.

## JSON contexts

Build the Providers project first. The generator inspects public Provider methods and base methods, then writes `JsonSerializerContexts` beneath the supplied source output. Review every `[JsonSerializable]` declaration and cover endpoint serialization before disabling reflection metadata.

## Stored-procedure mappers

Build the Data project first. The generator writes mapper sources beneath `Mappers` in the supplied source output. The current templates generate SQL Server parameter registration; PostgreSQL output remains an explicit design gap and must be implemented/reviewed manually with `INpgsqlParameterMapper` and `NpgsqlParameterMapperFactory`.

## OpenAPI client

Start the intended API profile and confirm that its OpenAPI document exposes only expected endpoints. The default settings generate the existing Angular TypeScript client and require the configured extension file beneath the output directory. Use `--settings <json>` for a reviewed alternative configuration. Generate only from a trusted endpoint and review the resulting client diff.

Generation deliberately loads client application assemblies or reads a supplied network document. Keep it an explicit developer/CI action; validation and review commands never trigger it.
