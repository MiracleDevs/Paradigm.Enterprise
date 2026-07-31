namespace Paradigm.Enterprise.Cli;

internal static class CommandLine
{
    internal const string Usage = """
        Paradigm.Enterprise CLI

        Usage:
          paradigm doctor [--project <path>] [--format text|json]
          paradigm api search <query> [--project <path>] [--framework <tfm>] [--package <name>] [--limit <1-100>] [--format text|json]
          paradigm api show <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
          paradigm api guide <symbol> [--project <path>] [--framework <tfm>] [--package <name>] [--format text|json]
          paradigm inspect [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm validate [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm checks list [--project <path>] [--config <path>] [--format text|json]
          paradigm checks run [--project <path>] [--framework <tfm>] [--config <path>] [--pack <id>] [--format text|json]
          paradigm packages check [--project <path>] [--framework <tfm>] [--config <path>] [--format text|json]
          paradigm packages audit [--project <path>] [--framework <tfm>] [--config <path>] [--warnings-as-errors] [--format text|json]
          paradigm --version
        """;

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
            var allowed = name == "checks" ? new[] { "list", "run" } : new[] { "check", "audit" };
            if (index >= args.Length || !allowed.Contains(args[index], StringComparer.Ordinal))
                return Fail($"Expected '{name} {string.Join("|", allowed)}'.", out error);
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
        string? pack = null;
        var limit = 20;
        var format = OutputFormat.Text;
        var warningsAsErrors = false;

        while (index < args.Length)
        {
            var option = args[index++];
            if (option == "--warnings-as-errors" && name == "packages audit")
            {
                warningsAsErrors = true;
                continue;
            }
            if (index >= args.Length)
                return Fail($"Option '{option}' requires a value.", out error);
            var value = args[index++];
            switch (option)
            {
                case "--project":
                    project = value;
                    break;
                case "--framework":
                    if (name is "doctor" or "checks list")
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
                case "--config" when name.StartsWith("checks ", StringComparison.Ordinal) ||
                                      name.StartsWith("packages ", StringComparison.Ordinal):
                    config = value;
                    break;
                case "--pack" when name == "checks run":
                    pack = value;
                    break;
                default:
                    return Fail($"Option '{option}' is not valid for '{name}'.", out error);
            }
        }

        ICliCommandOptions options = name switch
        {
            "doctor" => new DoctorOptions(project, format),
            "api search" => new ApiSearchOptions(query!, project, framework, package, limit, format),
            "api show" => new ApiShowOptions(query!, project, framework, package, format),
            "api guide" => new ApiGuideOptions(query!, project, framework, package, format),
            "inspect" => new InspectOptions(project, framework, format),
            "validate" => new ValidateOptions(project, framework, format),
            "checks list" => new ChecksListOptions(project, config, format),
            "checks run" => new ChecksRunOptions(project, framework, config, pack, format),
            "packages check" => new PackagesCheckOptions(project, framework, config, format),
            "packages audit" => new PackagesAuditOptions(project, framework, config, warningsAsErrors, format),
            _ => throw new InvalidOperationException($"No options model is registered for '{name}'.")
        };
        command = new(name, options);
        return true;
    }

    private static bool Fail(string message, out string? error)
    {
        error = message;
        return false;
    }
}
