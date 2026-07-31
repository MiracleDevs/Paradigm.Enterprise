if (args.Length >= 2 &&
    args[0] == "package" &&
    args[1] == "list" &&
    args.Contains("--project", StringComparer.Ordinal) &&
    args.Any(argument => argument is "--vulnerable" or "--deprecated" or "--outdated"))
{
    await Console.Out.WriteAsync("""{"version":1,"projects":[]}""");
    return 0;
}

await Console.Error.WriteAsync($"Unexpected deterministic dotnet-audit invocation: {string.Join(' ', args)}");
return 2;
