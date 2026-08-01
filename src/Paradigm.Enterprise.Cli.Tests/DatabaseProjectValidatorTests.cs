using System.Text.Json;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
public class DatabaseProjectValidatorTests
{
    #region Public Methods

    [TestMethod]
    public void Valid_sql_server_project_has_no_diagnostics()
    {
        WithTemporary(root =>
        {
            var (project, solution) = CreateSqlProject(root);
            Assert.AreEqual(0, Validate(project, solution, strict: true).Count);
        });
    }

    [TestMethod]
    public void Legacy_unique_prefix_is_warning_or_strict_error()
    {
        WithTemporary(root =>
        {
            var (project, solution) = CreateSqlProject(root, uniquePrefix: "UX");
            Assert.IsTrue(Validate(project, solution, strict: false).Any(item => item.Code == "PEDB101" && item.Severity == "warning"));
            Assert.IsTrue(Validate(project, solution, strict: true).Any(item => item.Code == "PEDB101" && item.Severity == "error"));
        });
    }

    [TestMethod]
    public void Status_catalog_and_history_require_assigned_seeded_mirrored_values()
    {
        WithTemporary(root =>
        {
            var (project, solution) = CreateSqlProject(root);
            var database = Path.GetDirectoryName(project)!;
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrder.sql"), SalesOrderSql());
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrderStatus.sql"), "CREATE TABLE [dbo].[SalesOrderStatus] ([Id] INT NOT NULL, [Name] NVARCHAR(50) NOT NULL, CONSTRAINT [PK_SalesOrderStatus] PRIMARY KEY ([Id]));");
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrderStatusHistory.sql"), "CREATE TABLE [dbo].[SalesOrderStatusHistory] ([Id] INT IDENTITY(1,1) NOT NULL, [SalesOrderId] INT NOT NULL, [StatusId] INT NOT NULL, [CreatedByUserId] INT NOT NULL, [CreationDate] DATETIMEOFFSET NOT NULL, CONSTRAINT [PK_SalesOrderStatusHistory] PRIMARY KEY ([Id]));");
            var seed = Path.Combine(database, "scripts", "postdeployment", "Sales", "SalesOrderStatusData.sql");
            File.WriteAllText(seed, "MERGE [dbo].[SalesOrderStatus] AS target USING (SELECT 1 Id) source ON source.Id=target.Id WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (1, 'Open');");
            File.AppendAllText(Path.Combine(database, "scripts", "postdeployment", "PostDeployment.sql"), Environment.NewLine + ":r .\\Sales\\SalesOrderStatusData.sql");
            File.WriteAllText(Path.Combine(root, "SalesOrderStatus.cs"), "internal enum SalesOrderStatus { Open = 1 }");
            var diagnostics = Validate(project, solution, strict: true);
            Assert.IsFalse(diagnostics.Any(item => item.Code is "PEDB109" or "PEDB110"), string.Join(Environment.NewLine, diagnostics.Select(item => item.Message)));
        });
    }

    [TestMethod]
    public void Missing_status_history_and_auto_incremented_catalog_are_reported()
    {
        WithTemporary(root =>
        {
            var (project, solution) = CreateSqlProject(root);
            var database = Path.GetDirectoryName(project)!;
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrder.sql"), SalesOrderSql());
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrderStatus.sql"), "CREATE TABLE [dbo].[SalesOrderStatus] ([Id] INT IDENTITY(1,1) NOT NULL, CONSTRAINT [PK_SalesOrderStatus] PRIMARY KEY ([Id]));");
            var diagnostics = Validate(project, solution, strict: true);
            Assert.IsTrue(diagnostics.Any(item => item.Code == "PEDB109"));
            Assert.IsTrue(diagnostics.Any(item => item.Code == "PEDB110"));
        });
    }

    [TestMethod]
    public void Status_seed_deletion_and_history_cascade_are_reported()
    {
        WithTemporary(root =>
        {
            var (project, solution) = CreateSqlProject(root);
            var database = Path.GetDirectoryName(project)!;
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrder.sql"), SalesOrderSql());
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrderStatus.sql"), "CREATE TABLE [dbo].[SalesOrderStatus] ([Id] INT NOT NULL, [Name] NVARCHAR(50) NOT NULL, CONSTRAINT [PK_SalesOrderStatus] PRIMARY KEY ([Id]));");
            File.WriteAllText(Path.Combine(database, "tables", "Sales", "SalesOrderStatusHistory.sql"), "CREATE TABLE [dbo].[SalesOrderStatusHistory] ([Id] INT IDENTITY(1,1) NOT NULL, [SalesOrderId] INT NOT NULL, [StatusId] INT NOT NULL, [CreatedByUserId] INT NOT NULL, [CreationDate] DATETIMEOFFSET NOT NULL, CONSTRAINT [PK_SalesOrderStatusHistory] PRIMARY KEY ([Id]), CONSTRAINT [FK_SalesOrderStatusHistory_SalesOrder] FOREIGN KEY ([SalesOrderId]) REFERENCES [dbo].[SalesOrder] ([Id]) ON DELETE CASCADE);");
            File.WriteAllText(Path.Combine(database, "scripts", "postdeployment", "Sales", "SalesOrderStatusData.sql"), "MERGE [dbo].[SalesOrderStatus] AS target USING (SELECT 1 Id) source ON source.Id=target.Id WHEN NOT MATCHED BY SOURCE THEN DELETE;");
            File.WriteAllText(Path.Combine(root, "SalesOrderStatus.cs"), "internal enum SalesOrderStatus { Open = 1 }");

            var diagnostics = Validate(project, solution, strict: true);
            Assert.IsTrue(diagnostics.Any(item => item.Code == "PEDB109" && item.Message.Contains("never be deleted", StringComparison.Ordinal)));
            Assert.IsTrue(diagnostics.Any(item => item.Code == "PEDB110" && item.Message.Contains("non-cascading", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void Pre_pre_deployment_must_be_registered_and_executed_before_publish()
    {
        WithTemporary(root =>
        {
            var (project, solution) = CreateSqlProject(root);
            var text = File.ReadAllText(project).Replace("    <Build Remove=\"scripts\\prepredeployment\\**\\*.sql\" />" + Environment.NewLine, "", StringComparison.Ordinal);
            File.WriteAllText(project, text);
            File.WriteAllText(Path.Combine(root, "Product.DatabaseBootstrap", "Program.cs"), "Console.WriteLine(\"no pre-publish step\");");
            var diagnostics = Validate(project, solution, strict: true);
            Assert.IsTrue(diagnostics.Any(item => item.Code == "PEDB105" && item.Message.Contains("PrePreDeployment", StringComparison.Ordinal)));
        });
    }

    [TestMethod]
    public void Valid_postgresql_project_allows_native_defaults()
    {
        WithTemporary(root => Assert.AreEqual(0, Validate(CreatePostgreSqlProject(root), null, strict: true).Count));
    }

    [TestMethod]
    public void Postgresql_detects_secret_duplicate_missing_order_and_noop_upsert()
    {
        WithTemporary(root =>
        {
            var project = CreatePostgreSqlProject(root);
            File.WriteAllText(project, """
                {
                  "databaseType": "PostgreSql",
                  "connectionString": "Host=localhost;Password=secret",
                  "generateScript": true,
                  "executeScript": "yes",
                  "outputFileName": "publish.sql",
                  "files": ["views/Security/UserView.sql", "tables/Security/User.sql", "tables/Security/User.sql", "tables/Missing.sql", "scripts/postdeployment/Security/RoleData.sql"],
                  "paths": ["missing-path"]
                }
                """);
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(project)!, "scripts", "postdeployment", "Security", "RoleData.sql"), "INSERT INTO \"Role\" (\"Id\") VALUES (1) ON CONFLICT (\"Id\") DO UPDATE SET \"Id\" = \"Role\".\"Id\";");
            var diagnostics = Validate(project, null, strict: true);
            foreach (var code in new[] { "PEDB003", "PEDB201", "PEDB202", "PEDB203", "PEDB106" })
                Assert.IsTrue(diagnostics.Any(item => item.Code == code), code);
            CollectionAssert.AreEqual(diagnostics.ToArray(), Validate(project, null, strict: true).ToArray());
        });
    }

    #endregion

    #region Private Methods

    private static IReadOnlyList<Diagnostic> Validate(string project, string? solution, bool strict) => new DatabaseProjectValidator().Validate(new(project, solution, strict, OutputFormat.Json), CancellationToken.None);

    private static void WithTemporary(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"paradigm-database-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            action(root);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static (string Project, string Solution) CreateSqlProject(string root, string uniquePrefix = "UQ")
    {
        var database = Path.Combine(root, "database");
        CreateFolders(database, includePrePre: true);
        var project = Path.Combine(database, "Product.Database.sqlproj");
        File.WriteAllText(project, """
            <Project DefaultTargets="Build">
              <Sdk Name="Microsoft.Build.Sql" Version="2.1.0" />
              <ItemGroup>
                <PreDeploy Include="scripts\predeployment\PreDeployment.sql" />
                <PostDeploy Include="scripts\postdeployment\PostDeployment.sql" />
                <Build Remove="scripts\prepredeployment\**\*.sql" />
                <None Include="scripts\prepredeployment\PrePreDeployment.sql" />
                <Build Remove="scripts\postdeployment\**\*.sql" />
                <Build Remove="scripts\maintenance\**\*.sql" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(database, "tables", "Security", "User.sql"), $"CREATE TABLE [dbo].[User] ([Id] INT IDENTITY(1,1) NOT NULL, [Email] NVARCHAR(200) NOT NULL, [CreatedByUserId] INT NULL, [CreationDate] DATETIMEOFFSET NOT NULL, [ModifiedByUserId] INT NULL, [ModificationDate] DATETIMEOFFSET NULL, [IsActive] BIT NOT NULL, CONSTRAINT [PK_User] PRIMARY KEY ([Id]), CONSTRAINT [{uniquePrefix}_User_Email] UNIQUE ([Email]));");
        File.WriteAllText(Path.Combine(database, "views", "Security", "UserView.sql"), "CREATE VIEW [dbo].[UserView] WITH SCHEMABINDING AS SELECT [Id], [Email] FROM [dbo].[User];");
        File.WriteAllText(Path.Combine(database, "scripts", "prepredeployment", "PrePreDeployment.sql"), "IF OBJECT_ID('[dbo].[Legacy]', 'U') IS NOT NULL DROP TABLE [dbo].[Legacy];");
        File.WriteAllText(Path.Combine(database, "scripts", "predeployment", "PreDeployment.sql"), "-- reserved");
        File.WriteAllText(Path.Combine(database, "scripts", "postdeployment", "PostDeployment.sql"), ":r .\\Security\\RoleData.sql");
        File.WriteAllText(Path.Combine(database, "scripts", "postdeployment", "Security", "RoleData.sql"), "MERGE [dbo].[Role] AS target USING (SELECT 1 Id) source ON source.Id=target.Id WHEN NOT MATCHED THEN INSERT (Id) VALUES (1);");
        var solution = Path.Combine(root, "Product.slnx");
        File.WriteAllText(solution, "<Solution><Project Path=\"database/Product.Database.sqlproj\" /></Solution>");
        var bootstrap = Path.Combine(root, "Product.DatabaseBootstrap");
        Directory.CreateDirectory(bootstrap);
        File.WriteAllText(Path.Combine(bootstrap, "Program.cs"), "var path = \"PrePreDeployment.sql\";");
        return (project, solution);
    }

    private static string CreatePostgreSqlProject(string root)
    {
        var database = Path.Combine(root, "database");
        CreateFolders(database, includePrePre: false);
        File.WriteAllText(Path.Combine(database, "tables", "Security", "User.sql"), "CREATE TABLE IF NOT EXISTS \"User\" (\"Id\" SERIAL NOT NULL, \"IsActive\" BOOLEAN NOT NULL DEFAULT true, CONSTRAINT \"PK_User\" PRIMARY KEY (\"Id\"));");
        File.WriteAllText(Path.Combine(database, "views", "Security", "UserView.sql"), "CREATE OR REPLACE VIEW \"UserView\" AS SELECT \"Id\" FROM \"User\";");
        File.WriteAllText(Path.Combine(database, "scripts", "predeployment", "Database.sql"), "SELECT 1;");
        File.WriteAllText(Path.Combine(database, "routines", "SearchUsers.sql"), "SELECT 1;");
        File.WriteAllText(Path.Combine(database, "scripts", "postdeployment", "Security", "RoleData.sql"), "INSERT INTO \"Role\" (\"Id\") VALUES (1) ON CONFLICT (\"Id\") DO UPDATE SET \"Id\" = EXCLUDED.\"Id\";");
        var project = Path.Combine(database, "project.jsonc");
        File.WriteAllText(project, JsonSerializer.Serialize(new
        {
            databaseType = "PostgreSql",
            generateScript = true,
            executeScript = true,
            outputFileName = "publish.sql",
            files = new[]
            {
                "scripts/predeployment/Database.sql",
                "tables/Security/User.sql",
                "views/Security/UserView.sql",
                "routines/SearchUsers.sql",
                "scripts/postdeployment/Security/RoleData.sql"
            }
        }));
        return project;
    }

    private static void CreateFolders(string database, bool includePrePre)
    {
        foreach (var folder in new[] { "tables/Security", "tables/Sales", "views/Security", "functions", "routines", "types", "scripts/predeployment", "scripts/postdeployment/Security", "scripts/postdeployment/Sales", "scripts/maintenance" })
            Directory.CreateDirectory(Path.Combine(database, folder.Replace('/', Path.DirectorySeparatorChar)));
        if (includePrePre)
            Directory.CreateDirectory(Path.Combine(database, "scripts", "prepredeployment"));
    }

    private static string SalesOrderSql() => "CREATE TABLE [dbo].[SalesOrder] ([Id] INT IDENTITY(1,1) NOT NULL, [StatusId] INT NOT NULL, [CreatedByUserId] INT NULL, [CreationDate] DATETIMEOFFSET NOT NULL, [ModifiedByUserId] INT NULL, [ModificationDate] DATETIMEOFFSET NULL, CONSTRAINT [PK_SalesOrder] PRIMARY KEY ([Id]));";

    #endregion
}