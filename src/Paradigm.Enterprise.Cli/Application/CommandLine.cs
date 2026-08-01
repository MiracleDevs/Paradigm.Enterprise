namespace Paradigm.Enterprise.Cli;

internal static class CommandLine
{
    #region Constants

    internal const string Usage = """
        Paradigm.Enterprise CLI

        Usage:
          paradigm doctor [--project <path>] [--format text|json]
          paradigm api search <query> [--project <path>] [--framework <tfm>] [--package <name>] [--limit <1-100>] [--format text|json]
          paradigm api show <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
          paradigm api guide <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
          paradigm inspect [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm validate [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm checks list [--format text|json]
          paradigm checks run [--project <path>] [--framework <tfm>] [--config <path>] [--format text|json]
          paradigm generate json --project-name <name> --assembly <dll> --output <directory> [--settings <json>] [--format text|json]
          paradigm generate mappers --project-name <name> --assembly <dll> --output <directory> [--settings <json>] [--format text|json]
          paradigm generate client --document <url> --output <directory> [--settings <json>] [--format text|json]
          paradigm scaffold solution --template-root <path> --name <name> --output <empty-directory> --paradigm-version <version> [--dry-run] [--format text|json]
          paradigm database validate --project <sqlproj-or-project.jsonc> [--solution <sln-or-slnx>] [--strict] [--format text|json]
          paradigm packages check [--project <path>] [--framework <tfm>] [--config <path>] [--format text|json]
          paradigm packages audit [--project <path>] [--framework <tfm>] [--config <path>] [--warnings-as-errors] [--format text|json]
          paradigm --version
        """;

    #endregion

    #region Public Methods

    public static bool TryParse(string[] args, out ParsedCommand? command, out string? error)
    {
        command = null;
        error = null;
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            command = new("help", new HelpOptions());
            return true;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            command = new("version", new VersionOptions());
            return true;
        }

        var index = 0;
        var name = args[index++];
        string? query = null;
        if (name == "api")
        {
            if (index >= args.Length || args[index] is not ("search" or "show" or "guide"))
                return Fail("Expected 'api search <query>', 'api show <symbol>', or 'api guide <symbol>'.", out error);
            name += " " + args[index++];
            if (index >= args.Length || args[index].StartsWith('-'))
                return Fail($"Expected a {(name.EndsWith("search") ? "query" : "symbol")}.", out error);
            query = args[index++];
        }
        else if (name is "checks" or "packages")
        {
            var allowed = name == "checks" ? new[]
            {
                "list",
                "run"
            }

            : new[]
            {
                "check",
                "audit"
            };
            if (index >= args.Length || !allowed.Contains(args[index], StringComparer.Ordinal))
                return Fail($"Expected '{name} {string.Join("|", allowed)}'.", out error);
            name += " " + args[index++];
        }
        else if (name == "generate")
        {
            if (index >= args.Length || args[index] is not ("json" or "mappers" or "client"))
                return Fail("Expected 'generate json|mappers|client'.", out error);
            name += " " + args[index++];
        }
        else if (name == "scaffold")
        {
            if (index >= args.Length || args[index] != "solution")
                return Fail("Expected 'scaffold solution'.", out error);
            name += " " + args[index++];
        }
        else if (name == "database")
        {
            if (index >= args.Length || args[index] != "validate")
                return Fail("Expected 'database validate'.", out error);
            name += " " + args[index++];
        }
        else if (name is not ("doctor" or "inspect" or "validate"))
        {
            return Fail($"Unknown command '{name}'.", out error);
        }

        string? project = null;
        string? framework = null;
        string? package = null;
        string? config = null;
        string? projectName = null;
        string? assemblyPath = null;
        string? outputPath = null;
        string? document = null;
        string? settingsPath = null;
        string? templateRoot = null;
        string? solutionName = null;
        string? paradigmVersion = null;
        string? solution = null;
        var limit = 20;
        var format = OutputFormat.Text;
        var warningsAsErrors = false;
        var dryRun = false;
        var strict = false;
        while (index < args.Length)
        {
            var option = args[index++];
            if (option == "--warnings-as-errors" && name == "packages audit")
            {
                warningsAsErrors = true;
                continue;
            }
            if (option == "--dry-run" && name == "scaffold solution")
            {
                dryRun = true;
                continue;
            }
            if (option == "--strict" && name == "database validate")
            {
                strict = true;
                continue;
            }

            if (index >= args.Length)
                return Fail($"Option '{option}' requires a value.", out error);
            var value = args[index++];
            switch (option)
            {
                case "--project":
                    if (name == "checks list" || name.StartsWith("generate ", StringComparison.Ordinal) || name == "scaffold solution")
                        return Fail($"Option '{option}' is not valid for '{name}'.", out error);
                    project = value;
                    break;
                case "--framework":
                    if (name is "doctor" or "checks list" or "database validate" or "scaffold solution" ||
                        name.StartsWith("generate ", StringComparison.Ordinal))
                        return Fail($"Option '{option}' is not valid for '{name}'.", out error);
                    framework = value;
                    break;
                case "--package" when name.StartsWith("api ", StringComparison.Ordinal):
                    package = value;
                    break;
                case "--limit" when name == "api search":
                    if (!int.TryParse(value, out limit) || limit is < 1 or > 100)
                        return Fail("--limit must be between 1 and 100.", out error);
                    break;
                case "--format":
                    if (!Enum.TryParse<OutputFormat>(value, true, out format))
                        return Fail("--format must be text or json.", out error);
                    break;
                case "--config" when name == "checks run" || name.StartsWith("packages ", StringComparison.Ordinal):
                    config = value;
                    break;
                case "--project-name" when name is "generate json" or "generate mappers":
                    projectName = value;
                    break;
                case "--assembly" when name is "generate json" or "generate mappers":
                    assemblyPath = value;
                    break;
                case "--output" when name.StartsWith("generate ", StringComparison.Ordinal):
                    outputPath = value;
                    break;
                case "--output" when name == "scaffold solution":
                    outputPath = value;
                    break;
                case "--document" when name == "generate client":
                    document = value;
                    break;
                case "--settings" when name.StartsWith("generate ", StringComparison.Ordinal):
                    settingsPath = value;
                    break;
                case "--template-root" when name == "scaffold solution":
                    templateRoot = value;
                    break;
                case "--name" when name == "scaffold solution":
                    solutionName = value;
                    break;
                case "--paradigm-version" when name == "scaffold solution":
                    paradigmVersion = value;
                    break;
                case "--solution" when name == "database validate":
                    solution = value;
                    break;
                default:
                    return Fail($"Option '{option}' is not valid for '{name}'.", out error);
            }
        }

        if (name.StartsWith("generate ", StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                return Fail($"'{name}' requires --output.", out error);
            if ((name is "generate json" or "generate mappers") && (string.IsNullOrWhiteSpace(projectName) || string.IsNullOrWhiteSpace(assemblyPath)))
                return Fail($"'{name}' requires --project-name and --assembly.", out error);
            if (name == "generate client" && string.IsNullOrWhiteSpace(document))
                return Fail("'generate client' requires --document.", out error);
        }
        if (name == "scaffold solution" && (string.IsNullOrWhiteSpace(templateRoot) || string.IsNullOrWhiteSpace(solutionName) || string.IsNullOrWhiteSpace(outputPath) || string.IsNullOrWhiteSpace(paradigmVersion)))
            return Fail("'scaffold solution' requires --template-root, --name, --output, and --paradigm-version.", out error);
        if (name == "database validate" && string.IsNullOrWhiteSpace(project))
            return Fail("'database validate' requires --project.", out error);

        ICliCommandOptions options = name switch
        {
            "doctor" => new DoctorOptions(project, format),
            "api search" => new ApiSearchOptions(query!, project, framework, package, limit, format),
            "api show" => new ApiShowOptions(query!, project, framework, package, format),
            "api guide" => new ApiGuideOptions(query!, project, framework, package, format),
            "inspect" => new InspectOptions(project, framework, format),
            "validate" => new ValidateOptions(project, framework, format),
            "checks list" => new ChecksListOptions(format),
            "checks run" => new ChecksRunOptions(project, framework, config, format),
            "generate json" or "generate mappers" or "generate client" => new GenerateOptions(name["generate ".Length..], projectName, assemblyPath, outputPath!, document, settingsPath, format),
            "scaffold solution" => new ScaffoldSolutionOptions(templateRoot!, solutionName!, outputPath!, paradigmVersion!, dryRun, format),
            "database validate" => new DatabaseValidateOptions(project!, solution, strict, format),
            "packages check" => new PackagesCheckOptions(project, framework, config, format),
            "packages audit" => new PackagesAuditOptions(project, framework, config, warningsAsErrors, format),
            _ => throw new InvalidOperationException($"No options model is registered for '{name}'.")
        };
        command = new(name, options);
        return true;
    }

    #endregion

    #region Private Methods

    private static bool Fail(string message, out string? error)
    {
        error = message;
        return false;
    }

    #endregion
}