using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Paradigm.Enterprise.Cli;

internal sealed class DatabaseProjectValidator
{
    #region Fields

    private readonly List<Diagnostic> diagnostics = [];
    private string project = null!;
    private string root = null!;
    private string? solution;
    private bool strict;

    #endregion

    #region Public Methods

    public IReadOnlyList<Diagnostic> Validate(DatabaseValidateOptions options, CancellationToken cancellationToken)
    {
        diagnostics.Clear();
        project = Path.GetFullPath(options.ProjectPath);
        root = Path.GetDirectoryName(project)!;
        solution = string.IsNullOrWhiteSpace(options.SolutionPath) ? null : Path.GetFullPath(options.SolutionPath);
        strict = options.Strict;

        if (!File.Exists(project))
            Add("PEDB001", "error", project, "Database project does not exist.");
        else if (Path.GetExtension(project).Equals(".sqlproj", StringComparison.OrdinalIgnoreCase))
            ValidateSqlServer(cancellationToken);
        else if (Path.GetExtension(project) is var extension && (extension.Equals(".json", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jsonc", StringComparison.OrdinalIgnoreCase)))
            ValidatePostgreSql(cancellationToken);
        else
            Add("PEDB001", "error", project, "Expected a .sqlproj or DbPublisher .json/.jsonc project.");

        return diagnostics
            .DistinctBy(item => (item.Code, item.Severity, item.Location, item.Message))
            .OrderBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();
    }

    #endregion

    #region Private Methods

    private void ValidateSqlServer(CancellationToken cancellationToken)
    {
        var raw = Read(project);
        XDocument document;
        try
        {
            document = XDocument.Parse(raw, LoadOptions.PreserveWhitespace);
        }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        {
            Add("PEDB002", "error", project, $"Invalid SQL project XML: {exception.Message}");
            return;
        }

        if (!raw.Contains("Microsoft.Build.Sql", StringComparison.Ordinal))
            Add("PEDB002", "error", project, "Use the SDK-style Microsoft.Build.Sql project format.");
        if (raw.Contains("preview", StringComparison.OrdinalIgnoreCase))
            Policy("PEDB002", project, "New projects must not pin a preview Microsoft.Build.Sql SDK.");

        ValidateLayout(sqlServer: true);
        ValidateSolutionMembership();
        ValidateBaseline();
        ValidateLiteralSecrets();
        ValidatePublishProfiles();
        ValidateSqlIncludes(document);
        ValidateSqlObjects(sqlServer: true, cancellationToken);
        ValidateSqlDeployment(raw);
        ValidateAdjacentBootstrap();
    }

    private void ValidatePostgreSql(CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(Read(project), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
        }
        catch (JsonException exception)
        {
            Add("PEDB201", "error", project, $"Invalid DbPublisher JSONC: {exception.Message}");
            return;
        }

        using (document)
        {
            var configuration = document.RootElement;
            if (configuration.ValueKind != JsonValueKind.Object)
            {
                Add("PEDB201", "error", project, "DbPublisher configuration must be a JSON object.");
                return;
            }

            if (!TryString(configuration, "databaseType", out var databaseType) || !databaseType.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
                Add("PEDB201", "error", project, "DbPublisher databaseType must be \"PostgreSql\".");

            var hasConnectionString = TryString(configuration, "connectionString", out var connectionString) && !string.IsNullOrWhiteSpace(connectionString);
            if (hasConnectionString)
                Add("PEDB003", "error", project, "Remove connectionString and supply Paradigm_ORM_ConnectionString securely.");

            if (!TryBoolean(configuration, "generateScript", out var generateScript) || !generateScript)
                Policy("PEDB201", project, "Enable generateScript so the aggregate publication can be reviewed.");
            if (configuration.TryGetProperty("executeScript", out var executeScript) && executeScript.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                Add("PEDB201", "error", project, "DbPublisher executeScript must be Boolean.");

            TryString(configuration, "outputFileName", out var outputFileName);
            if (generateScript && string.IsNullOrWhiteSpace(outputFileName))
                Add("PEDB201", "error", project, "Set outputFileName when generateScript is enabled.");
            else if (!string.IsNullOrWhiteSpace(outputFileName) && !IsWithin(root, Path.GetFullPath(Path.Combine(root, outputFileName))))
                Policy("PEDB201", project, "Keep generated publish output inside the database project.");

            var configuredFiles = ReadStringArray(configuration, "files", "DbPublisher files must be a string array.");
            var configuredPaths = ReadStringArray(configuration, "paths", "DbPublisher paths must be a string array.");
            var topDirectoryOnly = TryBoolean(configuration, "topDirectoryOnly", out var topOnly) && topOnly;
            if (configuration.TryGetProperty("topDirectoryOnly", out var topValue) && topValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                Add("PEDB201", "error", project, "DbPublisher topDirectoryOnly must be Boolean.");

            var effectiveFiles = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var configured in configuredFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = Path.GetFullPath(Path.Combine(root, Normalize(configured)));
                if (!IsWithin(root, path) || !File.Exists(path))
                {
                    Add("PEDB202", "error", project, $"Missing or escaping DbPublisher file: {configured}.");
                    continue;
                }
                AddPostgreSqlFile(path, effectiveFiles, seen);
            }

            foreach (var configured in configuredPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var directory = Path.GetFullPath(Path.Combine(root, Normalize(configured)));
                if (!IsWithin(root, directory) || !Directory.Exists(directory))
                {
                    Add("PEDB202", "error", project, $"Missing or escaping DbPublisher path: {configured}.");
                    continue;
                }
                var search = topDirectoryOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                foreach (var path in Directory.EnumerateFiles(directory, "*.sql", search).Order(StringComparer.OrdinalIgnoreCase))
                    AddPostgreSqlFile(path, effectiveFiles, seen);
            }

            var categories = effectiveFiles.Select(PostgreSqlCategory).Where(value => value.HasValue).Select(value => value!.Value).ToArray();
            if (!categories.SequenceEqual(categories.Order()))
                Add("PEDB203", "error", project, "Order DbPublisher files as predeployment, tables, upgrades, views, routines/functions, postdeployment.");

            ValidateLayout(sqlServer: false);
            ValidateSolutionMembership();
            ValidateBaseline();
            ValidateLiteralSecrets(hasConnectionString ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { project } : null);
            ValidateSqlObjects(sqlServer: false, cancellationToken);
        }
    }

    private void ValidateLayout(bool sqlServer)
    {
        var required = new List<string>
        {
            "tables",
            "views",
            "functions",
            "routines",
            "types",
            Path.Combine("scripts", "predeployment"),
            Path.Combine("scripts", "postdeployment"),
            Path.Combine("scripts", "maintenance")
        };
        if (sqlServer)
            required.Insert(5, Path.Combine("scripts", "prepredeployment"));
        foreach (var relative in required)
            if (!Directory.Exists(Path.Combine(root, relative)))
                Policy("PEDB004", Path.Combine(root, relative), $"Canonical database folder is missing: {relative.Replace(Path.DirectorySeparatorChar, '/')}.");
    }

    private void ValidateSolutionMembership()
    {
        if (solution is null)
            return;
        if (!File.Exists(solution))
        {
            Add("PEDB005", "error", solution, "Solution file does not exist.");
            return;
        }
        if (!Read(solution).Contains(Path.GetFileName(project), StringComparison.OrdinalIgnoreCase))
            Policy("PEDB005", solution, $"Solution does not reference {Path.GetFileName(project)}.");
    }

    private void ValidateBaseline()
    {
        var bootstrap = Path.Combine(root, "bootstrap");
        var direct = Directory.Exists(bootstrap) ? Directory.EnumerateFiles(bootstrap, "*.bacpac", SearchOption.TopDirectoryOnly).ToArray() : [];
        if (direct.Length > 1)
            Add("PEDB107", "error", bootstrap, "Keep at most one optional BACPAC baseline under bootstrap.");
        foreach (var baseline in Directory.EnumerateFiles(root, "*.bacpac", SearchOption.AllDirectories))
            if (!Path.GetDirectoryName(baseline)!.Equals(bootstrap, StringComparison.OrdinalIgnoreCase))
                Policy("PEDB107", baseline, "BACPAC baseline must be directly under the database bootstrap folder.");
    }

    private void ValidateSqlIncludes(XDocument document)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in document.Descendants().Where(element => element.Name.LocalName is "Build" or "None" or "PreDeploy" or "PostDeploy"))
        {
            var include = element.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include) || include.Contains("$(", StringComparison.Ordinal))
                continue;
            var normalized = Normalize(include);
            if (!seen.Add(normalized))
            {
                Policy("PEDB105", project, $"Duplicate SQL project include: {include}.");
                continue;
            }
            if (normalized.IndexOfAny(['*', '?']) >= 0)
                continue;
            if (!File.Exists(Path.GetFullPath(Path.Combine(root, normalized))))
                Policy("PEDB105", project, $"SQL project includes missing file: {include}.");
        }
    }

    private void ValidatePublishProfiles()
    {
        var profiles = Directory.EnumerateFiles(root, "*.publish.xml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "*.pubxml", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var unsafeValues = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["BlockOnPossibleDataLoss"] = false,
            ["DropObjectsNotInSource"] = true,
            ["AllowDropBlockingAssemblies"] = true
        };
        foreach (var path in profiles)
        {
            var text = Read(path);
            foreach (var (property, unsafeValue) in unsafeValues)
            {
                var match = Regex.Match(text, $"<{property}>\\s*(true|false)\\s*</{property}>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success && bool.Parse(match.Groups[1].Value) == unsafeValue)
                    Policy("PEDB108", path, $"Publish profile sets destructive {property}={match.Groups[1].Value}; require explicit reviewed use and do not make it an automatic default.");
            }
        }
    }

    private void ValidateLiteralSecrets(IReadOnlySet<string>? excluded = null)
    {
        var suffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".json", ".jsonc", ".xml", ".config", ".pubxml" };
        var pattern = new Regex("[\\\"']?(?:password|pwd)[\\\"']?\\s*[=:]\\s*[\\\"']?(?!\\$\\(|\\$\\{|\\[change\\]|<|\\*{3,})([^;\\s\\\"']+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
        {
            if (excluded?.Contains(path) == true || !suffixes.Contains(Path.GetExtension(path)))
                continue;
            if (pattern.IsMatch(Read(path)))
                Add("PEDB003", "error", path, "Configuration appears to contain a literal database password.");
        }
    }

    private void ValidateSqlObjects(bool sqlServer, CancellationToken cancellationToken)
    {
        ValidateObjectFiles(sqlServer);
        var tableRoot = Path.Combine(root, "tables");
        if (Directory.Exists(tableRoot))
            foreach (var path in Directory.EnumerateFiles(tableRoot, "*.sql", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var text = Read(path);
                foreach (var name in ConstraintNames(text))
                {
                    var prefix = name.Split('_', 2)[0].ToUpperInvariant();
                    if (prefix == "UX")
                        Policy("PEDB101", path, $"Preserve legacy {name}, but use UQ_ for new unique constraints.");
                    else if (prefix is not ("PK" or "FK" or "UQ" or "IX" or "DF" or "CK"))
                        Policy("PEDB101", path, $"Constraint {name} does not use a canonical prefix.");
                }

                foreach (var keyword in new[] { "PRIMARY KEY", "FOREIGN KEY" })
                {
                    var total = Regex.Matches(text, $"\\b{keyword}\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
                    var named = Regex.Matches(text, $"\\bCONSTRAINT\\s+(?:\\[[^]]+\\]|\"[^\"]+\"|[A-Za-z_][\\w$]*)\\s+{keyword}\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
                    if (total > named)
                        Policy("PEDB102", path, $"Name every {keyword.ToLowerInvariant()} constraint explicitly.");
                }

                if (sqlServer)
                {
                    var defaults = Regex.Matches(text, "\\bDEFAULT\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
                    var namedDefaults = Regex.Matches(text, "\\bCONSTRAINT\\s+(?:\\[[^]]+\\]|\"[^\"]+\"|[A-Za-z_][\\w$]*)\\s+DEFAULT\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Count;
                    if (defaults > namedDefaults)
                        Policy("PEDB102", path, "Name every default constraint explicitly.");
                }

                var audit = new[] { "CreatedByUserId", "CreationDate", "ModifiedByUserId", "ModificationDate" };
                var present = audit.Where(column => IdentifierPresent(text, column)).ToArray();
                if (present.Length > 0 && present.Length != audit.Length && !Path.GetFileNameWithoutExtension(path).EndsWith("StatusHistory", StringComparison.Ordinal))
                    Policy("PEDB103", path, "Use the complete canonical audit-field set or document why the table is not auditable.");
                foreach (var column in present)
                    if (Regex.IsMatch(text, $"(?:\\[{Regex.Escape(column)}\\]|\"{Regex.Escape(column)}\"|\\b{Regex.Escape(column)}\\b)[^,\\r\\n]*\\bDEFAULT\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                        Policy("PEDB103", path, $"Audit column {column} has a database default; verify that the database, not the Paradigm application, deliberately owns this value.");
                if (IdentifierPresent(text, "IsDeleted"))
                    Policy("PEDB103", path, "Use IsActive for new Paradigm logical lifecycle behavior; preserve IsDeleted only for compatibility.");

                ValidateIdentityAndStatusHistory(path, text, sqlServer);
            }

        if (sqlServer)
        {
            var viewRoot = Path.Combine(root, "views");
            if (Directory.Exists(viewRoot))
                foreach (var path in Directory.EnumerateFiles(viewRoot, "*.sql", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
                    if (!Read(path).Contains("SCHEMABINDING", StringComparison.OrdinalIgnoreCase))
                        Policy("PEDB104", path, "SQL Server views should use SCHEMABINDING unless a documented dependency prevents it.");
        }

        var postRoot = Path.Combine(root, "scripts", "postdeployment");
        if (Directory.Exists(postRoot))
            foreach (var path in Directory.EnumerateFiles(postRoot, "*.sql", SearchOption.AllDirectories).Where(path => !Path.GetFileName(path).Equals("PostDeployment.sql", StringComparison.OrdinalIgnoreCase)).Order(StringComparer.OrdinalIgnoreCase))
            {
                var text = Read(path);
                if (!Regex.IsMatch(text, "\\bMERGE\\b|\\bIF\\s+NOT\\s+EXISTS\\b|\\bON\\s+CONFLICT\\b|\\bWHERE\\s+NOT\\s+EXISTS\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    Add("PEDB106", "warning", path, "Post-deployment data script has no recognized idempotency signal; review manually.");
                if (Regex.IsMatch(text, "WHEN\\s+NOT\\s+MATCHED\\s+BY\\s+SOURCE\\s+THEN\\s+DELETE", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    Add("PEDB106", "warning", path, "Source-deleting MERGE requires an explicitly authoritative closed catalog.");
                if (Regex.IsMatch(text, "\\bON\\s+CONFLICT\\b[\\s\\S]*?\\bDO\\s+UPDATE\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && !Regex.IsMatch(text, "\\bEXCLUDED\\s*\\.", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    Add("PEDB106", "warning", path, "ON CONFLICT DO UPDATE does not reference EXCLUDED; verify that the update is not a no-op.");
            }
    }

    private void ValidateObjectFiles(bool sqlServer)
    {
        var pattern = new Regex("\\bCREATE\\s+(?:OR\\s+(?:ALTER|REPLACE)\\s+)?(?:TABLE|VIEW|TYPE|FUNCTION|PROCEDURE|PROC)\\s+(?:IF\\s+NOT\\s+EXISTS\\s+)?((?:\\[[^]]+\\]|\"[^\"]+\"|[A-Za-z_][\\w$]*)(?:\\.(?:\\[[^]]+\\]|\"[^\"]+\"|[A-Za-z_][\\w$]*))?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var folder in new[] { "tables", "views", "functions", "routines", "types" })
        {
            var directory = Path.Combine(root, folder);
            if (!Directory.Exists(directory))
                continue;
            foreach (var path in Directory.EnumerateFiles(directory, "*.sql", SearchOption.AllDirectories).Order(StringComparer.OrdinalIgnoreCase))
            {
                var objects = pattern.Matches(Read(path)).Select(match => match.Groups[1].Value).ToArray();
                if (objects.Length > 1)
                    Policy("PEDB100", path, "Keep one semantic database object per file.");
                foreach (var qualifiedName in objects)
                {
                    var parts = qualifiedName.Split('.');
                    if (sqlServer && parts.Length < 2)
                        Policy("PEDB100", path, "SQL Server objects must use an explicit owned schema.");
                    var objectName = parts[^1].Trim('[', ']', '"');
                    if (!objectName.Equals(Path.GetFileNameWithoutExtension(path), StringComparison.OrdinalIgnoreCase))
                        Policy("PEDB100", path, $"File name must match object {objectName}.");
                }
            }
        }
    }

    private void ValidateIdentityAndStatusHistory(string path, string text, bool sqlServer)
    {
        var tableName = Path.GetFileNameWithoutExtension(path);
        var identity = sqlServer
            ? Regex.IsMatch(text, "\\bIDENTITY\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            : Regex.IsMatch(text, "\\b(?:SERIAL|BIGSERIAL)\\b|\\bGENERATED\\s+(?:BY\\s+DEFAULT|ALWAYS)\\s+AS\\s+IDENTITY\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var isStatusCatalog = tableName.EndsWith("Status", StringComparison.OrdinalIgnoreCase);
        var isStatusHistory = tableName.EndsWith("StatusHistory", StringComparison.OrdinalIgnoreCase);

        if (isStatusCatalog)
        {
            if (identity)
                Policy("PEDB109", path, "Seeded system/status tables use stable assigned identifiers and must not auto-increment Id.");
            var seedName = tableName + "Data.sql";
            var postRoot = Path.Combine(root, "scripts", "postdeployment");
            var seed = Directory.Exists(postRoot)
                ? Directory.EnumerateFiles(postRoot, "*.sql", SearchOption.AllDirectories).FirstOrDefault(candidate => Path.GetFileName(candidate).Equals(seedName, StringComparison.OrdinalIgnoreCase))
                : null;
            if (seed is null)
                Policy("PEDB109", path, $"System/status table {tableName} requires rerunnable post-deployment seed script {seedName}.");
            else if (Regex.IsMatch(Read(seed), "WHEN\\s+NOT\\s+MATCHED\\s+BY\\s+SOURCE\\s+THEN\\s+DELETE", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                Policy("PEDB109", seed, "Published status identifiers must never be deleted, reused, or renumbered; retire statuses with IsActive.");
            if (solution is not null && !SolutionContainsEnum(tableName))
                Policy("PEDB109", path, $"Service-side .NET enum {tableName} must mirror the seeded numeric identifiers and stable machine codes.");
            return;
        }

        if (!isStatusHistory && IdentifierPresent(text, "StatusId"))
        {
            var historyName = tableName + "StatusHistory";
            var tables = Path.Combine(root, "tables");
            var history = Directory.Exists(tables) ? Directory.EnumerateFiles(tables, historyName + ".sql", SearchOption.AllDirectories).FirstOrDefault() : null;
            if (history is null)
            {
                Policy("PEDB110", path, $"Stateful entity {tableName} requires append-only transition table {historyName}.");
                return;
            }
            var historyText = Read(history);
            foreach (var column in new[] { tableName + "Id", "StatusId", "CreatedByUserId", "CreationDate" })
                if (!IdentifierPresent(historyText, column))
                    Policy("PEDB110", history, $"Status history table {historyName} is missing {column}.");
            var historyIdentity = sqlServer
                ? Regex.IsMatch(historyText, "\\bIDENTITY\\s*\\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                : Regex.IsMatch(historyText, "\\b(?:SERIAL|BIGSERIAL)\\b|\\bGENERATED\\s+(?:BY\\s+DEFAULT|ALWAYS)\\s+AS\\s+IDENTITY\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!historyIdentity)
                Policy("PEDB110", history, $"Status history table {historyName} requires an auto-incrementing Id.");
            if (Regex.IsMatch(historyText, "\\bON\\s+DELETE\\s+CASCADE\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                Policy("PEDB110", history, $"Status history table {historyName} requires non-cascading foreign keys so transition facts survive lifecycle changes.");
        }
    }

    private void ValidateSqlDeployment(string projectText)
    {
        var pre = Path.Combine(root, "scripts", "predeployment", "PreDeployment.sql");
        var post = Path.Combine(root, "scripts", "postdeployment", "PostDeployment.sql");
        var prePre = Path.Combine(root, "scripts", "prepredeployment", "PrePreDeployment.sql");
        if (File.Exists(pre) && !Regex.IsMatch(projectText, "<PreDeploy\\s+Include=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB105", project, "Register PreDeployment.sql as PreDeploy.");
        if (File.Exists(post) && !Regex.IsMatch(projectText, "<PostDeploy\\s+Include=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB105", project, "Register PostDeployment.sql as PostDeploy.");
        if (!File.Exists(prePre))
            Policy("PEDB105", prePre, "Add an idempotent PrePreDeployment.sql root for work that must run before SqlPackage creates its deployment plan.");
        else
        {
            if (!Regex.IsMatch(projectText, "<Build\\s+Remove=\"[^\"]*prepredeployment[^\"]*\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                Policy("PEDB105", project, "Exclude PrePreDeployment.sql from model build.");
            if (!Regex.IsMatch(projectText, "<None\\s+Include=\"[^\"]*PrePreDeployment\\.sql\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                Policy("PEDB105", project, "Register PrePreDeployment.sql as a non-model bootstrap artifact, not DACPAC PreDeploy.");
        }

        ValidateDeploymentChildren(post, "postdeployment", projectText);
        var maintenance = Path.Combine(root, "scripts", "maintenance");
        if (Directory.Exists(maintenance) && Directory.EnumerateFiles(maintenance, "*.sql", SearchOption.AllDirectories).Any() && !Regex.IsMatch(projectText, "<Build\\s+Remove=\"[^\"]*maintenance[^\"]*\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB105", project, "Exclude maintenance scripts from model build.");
    }

    private void ValidateDeploymentChildren(string rootScript, string folderName, string projectText)
    {
        var directory = Path.GetDirectoryName(rootScript)!;
        if (!Directory.Exists(directory))
            return;
        var rootText = File.Exists(rootScript) ? Read(rootScript) : "";
        var removed = Regex.IsMatch(projectText, $"<Build\\s+Remove=\"[^\"]*{folderName}[^\"]*\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        foreach (var path in Directory.EnumerateFiles(directory, "*.sql", SearchOption.AllDirectories).Where(path => !path.Equals(rootScript, StringComparison.OrdinalIgnoreCase)))
        {
            if (!rootText.Contains(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase))
                Policy("PEDB105", path, $"{Path.GetFileName(rootScript)} does not include this data script with :r.");
            if (!removed)
                Policy("PEDB105", path, $"Exclude subordinate {folderName} scripts from model build.");
        }
    }

    private void ValidateAdjacentBootstrap()
    {
        if (solution is null || !File.Exists(solution))
            return;
        var common = CommonAncestor(root, Path.GetDirectoryName(solution)!);
        if (common is null || common == Path.GetPathRoot(common))
            return;
        var candidates = Directory.EnumerateFiles(common, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".ps1" or ".json" or ".jsonc")
            .Where(path => !PathParts(path).Any(part => part is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "dist" or "artifacts"))
            .Where(path => PathParts(Path.GetDirectoryName(path)!).Any(part => part.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) || part.Contains("apphost", StringComparison.OrdinalIgnoreCase) || part.Contains("publish", StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var texts = candidates.ToDictionary(path => path, Read, StringComparer.OrdinalIgnoreCase);
        foreach (var (path, text) in texts)
        {
            if (Regex.IsMatch(text, "BlockOnPossibleDataLoss\\s*[=:]\\s*(?:False|false|0)", RegexOptions.CultureInvariant))
                Add("PEDB108", "error", path, "Automatic database publication must not disable possible-data-loss blocking.");
            if (Regex.IsMatch(text, "tool\\s+install[^\\r\\n]*(?:--global|-g)[^\\r\\n]*sqlpackage", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                Add("PEDB108", "error", path, "Do not install SqlPackage from a running bootstrap; install the pinned tool while building the bootstrap image.");
        }

        var prePre = Path.Combine(root, "scripts", "prepredeployment", "PrePreDeployment.sql");
        if (File.Exists(prePre) && !texts.Values.Any(text => text.Contains("PrePreDeployment.sql", StringComparison.OrdinalIgnoreCase)))
            Policy("PEDB105", prePre, "The finite database bootstrap must execute PrePreDeployment.sql before SqlPackage creates its deployment plan.");

        var topologyRoot = Path.GetFileName(common).Equals("src", StringComparison.OrdinalIgnoreCase) && Directory.GetParent(common) is { } applicationRoot
            ? applicationRoot.FullName
            : common;
        ValidateContainerizedAspireBootstrap(topologyRoot);
    }

    private void ValidateContainerizedAspireBootstrap(string common)
    {
        var bootstrapDirectory = Directory.EnumerateDirectories(common, "*", SearchOption.AllDirectories)
            .Where(path => !PathParts(path).Any(part => part is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts"))
            .FirstOrDefault(path => Path.GetFileName(path).EndsWith(".DatabaseBootstrap", StringComparison.OrdinalIgnoreCase));
        if (bootstrapDirectory is null)
            return;

        var appHostFiles = Directory.EnumerateFiles(common, "*.cs", SearchOption.AllDirectories)
            .Where(path => !PathParts(path).Any(part => part is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts"))
            .Where(path => PathParts(Path.GetDirectoryName(path)!).Any(part => part.Contains("apphost", StringComparison.OrdinalIgnoreCase)))
            .Select(path => (Path: path, Text: Read(path)))
            .ToArray();
        if (appHostFiles.Length == 0)
            return;
        var appHostPath = appHostFiles.FirstOrDefault(candidate => Path.GetFileName(candidate.Path).Equals("Program.cs", StringComparison.OrdinalIgnoreCase)).Path
            ?? appHostFiles[0].Path;
        var appHostText = string.Join(Environment.NewLine, appHostFiles.Select(candidate => candidate.Text));

        if (Regex.IsMatch(appHostText, "AddProject\\s*<[^>]*DatabaseBootstrap", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB111", appHostPath, "Aspire SQL Server bootstrap must be a Dockerfile resource, not a host-process project resource.");
        if (!appHostText.Contains("AddDockerfile", StringComparison.Ordinal))
            Policy("PEDB111", appHostPath, "Register the finite SQL Server bootstrap with AddDockerfile so database tools remain inside Docker.");
        if (!appHostText.Contains("WaitForCompletion", StringComparison.Ordinal))
            Policy("PEDB111", appHostPath, "Make API startup use WaitForCompletion for the finite database-bootstrap container.");

        var dockerfile = Directory.EnumerateFiles(bootstrapDirectory, "Dockerfile", SearchOption.TopDirectoryOnly).SingleOrDefault();
        if (dockerfile is null)
        {
            Policy("PEDB111", bootstrapDirectory, "Add a database-bootstrap Dockerfile that builds the DACPAC and owns SQLCMD 18 plus pinned SqlPackage.");
            return;
        }

        var dockerfileText = Read(dockerfile);
        if (!dockerfileText.Contains("mssql-tools18", StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(dockerfileText, "Version\\s+18|Version 18|\\^Version 18", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB111", dockerfile, "Install SQLCMD through mssql-tools18 and verify major version 18 while building the bootstrap image.");
        if (!dockerfileText.Contains("Microsoft.SqlPackage", StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(dockerfileText, "(?:SQLPACKAGE_VERSION\\s*=|--version\\s+[^\\s]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB111", dockerfile, "Install a pinned Microsoft.SqlPackage version while building the bootstrap image.");
        if (!Regex.IsMatch(dockerfileText, "dotnet\\s+build[^\\r\\n]*(?:\\.sqlproj|DATABASE_PROJECT)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            Policy("PEDB111", dockerfile, "Build the SQL project and verify its DACPAC during bootstrap image construction.");

        foreach (var starter in Directory.EnumerateFiles(common, "start.sh", SearchOption.AllDirectories)
                     .Where(path => !PathParts(path).Any(part => part is "bin" or "obj" or ".git" or ".vs" or "node_modules" or "artifacts")))
        {
            var starterText = Read(starter);
            if (starterText.Contains("sqlcmd", StringComparison.OrdinalIgnoreCase) || starterText.Contains("sqlpackage", StringComparison.OrdinalIgnoreCase))
                Policy("PEDB111", starter, "Do not require or inspect SQLCMD or SqlPackage on the developer host; the bootstrap image owns both tools.");
        }
    }

    private bool SolutionContainsEnum(string enumName)
    {
        if (solution is null)
            return false;
        var common = CommonAncestor(root, Path.GetDirectoryName(solution)!);
        if (common is null)
            return false;
        var pattern = new Regex($"\\benum\\s+{Regex.Escape(enumName)}\\b", RegexOptions.CultureInvariant);
        return Directory.EnumerateFiles(common, "*.cs", SearchOption.AllDirectories)
            .Where(path => !PathParts(path).Any(part => part is "bin" or "obj" or ".git" or ".vs"))
            .Any(path => pattern.IsMatch(Read(path)));
    }

    private IReadOnlyList<string> ReadStringArray(JsonElement configuration, string property, string error)
    {
        if (!configuration.TryGetProperty(property, out var value))
            return [];
        if (value.ValueKind != JsonValueKind.Array || value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String))
        {
            Add("PEDB202", "error", project, error);
            return [];
        }
        return value.EnumerateArray().Select(item => item.GetString()!).ToArray();
    }

    private void AddPostgreSqlFile(string path, ICollection<string> effectiveFiles, ISet<string> seen)
    {
        if (!seen.Add(path))
        {
            Add("PEDB202", "error", project, $"Duplicate DbPublisher file: {Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')}.");
            return;
        }
        effectiveFiles.Add(Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'));
    }

    private static int? PostgreSqlCategory(string path)
    {
        var normalized = path.Replace('\\', '/').ToLowerInvariant();
        if (normalized.Contains("/predeployment/", StringComparison.Ordinal) || normalized.StartsWith("scripts/predeployment/", StringComparison.Ordinal))
            return 0;
        if (normalized.StartsWith("tables/", StringComparison.Ordinal))
            return 1;
        if (normalized.StartsWith("upgrades/", StringComparison.Ordinal) || normalized.Contains("/upgrades/", StringComparison.Ordinal))
            return 2;
        if (normalized.StartsWith("views/", StringComparison.Ordinal))
            return 3;
        if (normalized.StartsWith("functions/", StringComparison.Ordinal) || normalized.StartsWith("routines/", StringComparison.Ordinal))
            return 4;
        if (normalized.Contains("/postdeployment/", StringComparison.Ordinal) || normalized.StartsWith("scripts/postdeployment/", StringComparison.Ordinal))
            return 5;
        return null;
    }

    private static IReadOnlyList<string> ConstraintNames(string text)
    {
        var pattern = new Regex("\\bCONSTRAINT\\s+(?:\\[([^]]+)\\]|\"([^\"]+)\"|([A-Za-z_][\\w$]*))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return pattern.Matches(text).Select(match => match.Groups.Cast<Group>().Skip(1).First(group => group.Success).Value).ToArray();
    }

    private static bool IdentifierPresent(string text, string identifier) => Regex.IsMatch(text, $"(?:\\[{Regex.Escape(identifier)}\\]|\"{Regex.Escape(identifier)}\"|\\b{Regex.Escape(identifier)}\\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static bool TryString(JsonElement element, string property, out string value)
    {
        value = "";
        if (!element.TryGetProperty(property, out var propertyValue) || propertyValue.ValueKind != JsonValueKind.String)
            return false;
        value = propertyValue.GetString() ?? "";
        return true;
    }

    private static bool TryBoolean(JsonElement element, string property, out bool value)
    {
        value = false;
        if (!element.TryGetProperty(property, out var propertyValue) || propertyValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return false;
        value = propertyValue.GetBoolean();
        return true;
    }

    private void Policy(string code, string path, string message) => Add(code, strict ? "error" : "warning", path, message);
    private void Add(string code, string severity, string path, string message)
    {
        var fullPath = Path.GetFullPath(path);
        var location = IsWithin(root, fullPath) ? Path.GetRelativePath(root, fullPath) : fullPath;
        diagnostics.Add(new(code, severity, message, location));
    }

    private static bool IsWithin(string parent, string child)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(parent), Path.GetFullPath(child));
        return relative == "." || (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal));
    }

    private static string? CommonAncestor(string first, string second)
    {
        var current = new DirectoryInfo(Path.GetFullPath(first));
        var target = Path.GetFullPath(second);
        while (current is not null)
        {
            if (IsWithin(current.FullName, target))
                return current.FullName;
            current = current.Parent;
        }
        return null;
    }

    private static IEnumerable<string> PathParts(string path) => path.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).Select(part => part.ToLowerInvariant());
    private static string Normalize(string path) => path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
    private static string Read(string path) => File.ReadAllText(path);

    #endregion
}
