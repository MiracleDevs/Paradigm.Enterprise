namespace BeaconAr.AppHost;

internal static class DotEnvLoader
{
    #region Public Methods

    public static void Load(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line[7..].TrimStart();
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new InvalidDataException($"Malformed .env line for key position {separator}.");
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (!seen.Add(key))
            {
                throw new InvalidDataException($"Duplicate .env key '{key}'.");
            }

            if (value.Length >= 2 && ((value[0] == '\"' && value[^1] == '\"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    #endregion
}
