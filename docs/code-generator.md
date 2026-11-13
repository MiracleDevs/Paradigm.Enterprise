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
  --database-provider mysql `
  --output C:/work/Sample.Data

dotnet tool run paradigm generate client `
  --document https://localhost:7001/swagger/v1/swagger.json `
  --output C:/work/client/src/generated
```

Assembly input and source output are separate options. This fixes the former path ambiguity where one value was treated as both a DLL and a directory. A failed mode returns a nonzero result instead of logging an error and continuing as though generation succeeded.

## JSON contexts

Build the Providers project first. The generator inspects public Provider methods and base methods, then writes `JsonSerializerContexts` beneath the supplied source output. Review every `[JsonSerializable]` declaration and cover endpoint serialization before disabling reflection metadata.

## Stored-procedure mappers

Build the Data project first. The generator writes mapper sources beneath `Mappers` in the supplied source output. Select `--database-provider mysql` for Oracle MySQL, `postgresql` for Npgsql, or `sqlserver` for SQL Server. Omitting the option retains SQL Server as the default. The option is accepted only by `generate mappers`; invalid values fail before generation starts.

Input assemblies must have their runtime dependencies available alongside them. For a class library whose normal build does not copy those dependencies, build with `--property:CopyLocalLockFileAssemblies=true` or supply the assembly from the consuming host's build output. The generator loads the trusted assembly to find the selected provider's typed routine bases; it does not query the live database or infer column types from a connection string.

Generated parameter mappers and their registration use the selected provider's framework base/factory. Reader mappers use the shared Data contracts. Review the generated mappings against the database routine, particularly nullability, Guid representation, temporal values, and provider-specific parameter types. Custom database type metadata and output-parameter binding remain application-owned. Use separate outputs for independently owned provider/context configurations.

Generation validates the complete mapper set before writing files and refuses to replace handwritten source. `Mappers/.paradigm-mappers.json` records the exact files it owns. Later runs remove only stale files listed in that manifest that still carry this generator's ownership markers; handwritten partials and unrelated files remain untouched. Older output without a manifest needs manual review when removing obsolete mapper files. The tool never recursively deletes a mixed handwritten directory.

Scalar results and concrete `List<T>` results use the shared mapping rules. Object results need an accessible parameterless constructor and supported writable properties; ordinary property/constructor defaults survive missing or database-null fields. Unmappable required members, arrays used as result sets, and nested generic result types are rejected before output mutation. MySQL offset-preserving dates and composite/collection parameters require explicit application mappings rather than guessed conversions.

## OpenAPI client

Start the intended API profile and confirm that its OpenAPI document exposes only expected endpoints. The default settings generate the existing Angular TypeScript client and require the configured extension file beneath the output directory. Use `--settings <json>` for a reviewed alternative configuration. Generate only from a trusted endpoint and review the resulting client diff.

Generation deliberately loads client application assemblies or reads a supplied network document. Keep it an explicit developer/CI action; validation and review commands never trigger it.
