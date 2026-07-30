namespace Paradigm.Enterprise.Cli;

internal static class CommandLine
{
    internal const string Usage = """
        Paradigm.Enterprise CLI

        Usage:
          paradigm doctor [--project <path>] [--format text|json]
          paradigm api search <query> [--project <path>] [--framework <tfm>] [--package <name>] [--limit <1-100>] [--format text|json]
          paradigm api show <symbol> [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm inspect [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm validate [--project <path>] [--framework <tfm>] [--format text|json]
          paradigm --version
        """;

    public static bool TryParse(string[] args, out ParsedCommand? command, out string? error)
    {
        command = null;
        error = null;
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            command = new("help", null, null, null, null, 20, OutputFormat.Text);
            return true;
        }

        if (args.Length == 1 && args[0] == "--version")
        {
            command = new("version", null, null, null, null, 20, OutputFormat.Text);
            return true;
        }

        var index = 0;
        var name = args[index++];
        string? query = null;
        if (name == "api")
        {
            if (index >= args.Length || args[index] is not ("search" or "show"))
                return Fail("Expected 'api search <query>' or 'api show <symbol>'.", out error);
            name += " " + args[index++];
            if (index >= args.Length || args[index].StartsWith('-'))
                return Fail($"Expected a {(name.EndsWith("search") ? "query" : "symbol")}.", out error);
            query = args[index++];
        }
        else if (name is not ("doctor" or "inspect" or "validate"))
        {
            return Fail($"Unknown command '{name}'.", out error);
        }

        string? project = null;
        string? framework = null;
        string? package = null;
        var limit = 20;
        var format = OutputFormat.Text;

        while (index < args.Length)
        {
            var option = args[index++];
            if (index >= args.Length)
                return Fail($"Option '{option}' requires a value.", out error);
            var value = args[index++];
            switch (option)
            {
                case "--project":
                    project = value;
                    break;
                case "--framework":
                    if (name == "doctor")
                        return Fail($"Option '{option}' is not valid for '{name}'.", out error);
                    framework = value;
                    break;
                case "--package" when name == "api search":
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
                default:
                    return Fail($"Option '{option}' is not valid for '{name}'.", out error);
            }
        }

        command = new(name, query, project, framework, package, limit, format);
        return true;
    }

    private static bool Fail(string message, out string? error)
    {
        error = message;
        return false;
    }
}
