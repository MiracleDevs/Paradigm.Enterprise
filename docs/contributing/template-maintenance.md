# Maintain the Visual Studio template

The [Visual Studio template repository](https://github.com/MiracleDevs/Paradigm.Web.ApiTemplate) owns the project-group archive used to start new APIs. Template maintenance is separate from application development and from Enterprise package releases.

The build script can synchronize the authoring source into the template folder, replace the root namespace with Visual Studio parameters, generate per-project template manifests, create the archive, and install it locally.

Run the script from a shell supported by the repository:

```bash
./build/build-template.sh --no-install
```

Use `--sync-only` to refresh template content without packaging, or `--install-only` to install an existing archive. Close Visual Studio before installation because template caches and files may be in use.

## Review generated structure

The root project-group manifest is the authority for the solution users receive. Confirm that each project appears in the intended solution folder and that project references point inward through Interfaces, Domain, Data, Providers, and WebApi.

The authoring solution can differ from the generated solution. Validate the archive by creating a new solution through Visual Studio rather than assuming the authoring `.slnx` proves the package is correct.

## Validate the generated solution

Create a solution with a neutral test name. Restore and build it. Add a local connection string, reverse engineer a minimal database selection, build generated interfaces, and run the host. Install the Paradigm CLI through a local manifest and exercise each required `paradigm generate` mode only after its input assemblies exist.

Review host defaults before publishing a new template version. CORS, Swagger, authentication scaffolding, health output, JSON reflection settings, and middleware order are application policy and must be safe starting points.

Remove application-local generator scaffolds from new template output. Client projects should use the single installable CLI so fixes, command names, and generation settings have one owner. Keep any temporary migration shim explicit and do not publish it as a second tool.
