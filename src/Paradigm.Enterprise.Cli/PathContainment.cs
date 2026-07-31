namespace Paradigm.Enterprise.Cli;

internal static class PathContainment
{
    public static bool TryResolveWithin(
        string root,
        string relativePath,
        out string resolvedPath,
        out string? error)
    {
        try
        {
            var canonicalRoot = Canonicalize(root);
            var candidate = Path.GetFullPath(Path.Combine(root, relativePath));
            if (OperatingSystem.IsWindows() && Path.GetExtension(candidate).Length == 0 &&
                File.Exists(candidate + ".exe"))
                candidate += ".exe";
            resolvedPath = Canonicalize(candidate);
            var relative = Path.GetRelativePath(canonicalRoot, resolvedPath);
            var outside = Path.IsPathRooted(relative) ||
                          relative.Equals("..", StringComparison.Ordinal) ||
                          relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                          relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
            error = outside ? "resolved path is outside the canonical workspace" : null;
            return !outside;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                              ArgumentException or NotSupportedException)
        {
            resolvedPath = Path.GetFullPath(Path.Combine(root, relativePath));
            error = exception.Message;
            return false;
        }
    }

    internal static string Canonicalize(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)
                   ?? throw new ArgumentException($"Path '{path}' has no filesystem root.", nameof(path));
        var current = root;
        foreach (var segment in fullPath[root.Length..]
                     .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                         StringSplitOptions.RemoveEmptyEntries))
        {
            var next = Path.Combine(current, segment);
            FileSystemInfo info = Directory.Exists(next)
                ? new DirectoryInfo(next)
                : new FileInfo(next);
            if (info.Exists && info.LinkTarget is not null)
                current = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? next;
            else
                current = next;
        }
        return Path.GetFullPath(current);
    }
}
