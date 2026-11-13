namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[DoNotParallelize]
public class GenerateCommandTests
{
    #region Public Methods

    [TestMethod]
    public async Task Json_generation_uses_separate_assembly_and_source_output_paths()
    {
        var repository = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var assembly = Path.Combine(repository, "src", "Paradigm.Enterprise.Cli.Tests", "Fixtures", "GoodPractices", "bin", configuration, "net10.0", "GoodPractices.dll");
        var output = Path.Combine(Path.GetTempPath(), $"paradigm-generation-{Guid.NewGuid():N}");
        try
        {
            using var writer = new StringWriter();
            var exit = await TestCliApplication.RunAsync(["generate", "json", "--project-name", "GoodPractices", "--assembly", assembly, "--output", output, "--format", "json"], writer, TextWriter.Null);
            Assert.AreEqual(0, exit, writer.ToString());
            Assert.IsTrue(Directory.Exists(Path.Combine(output, "JsonSerializerContexts")));
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("sqlserver")]
    [DataRow("postgresql")]
    [DataRow("mysql")]
    public async Task Mapper_generation_is_provider_specific_compilable_and_repeatable(string provider)
    {
        var assembly = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var output = Path.Combine(Path.GetTempPath(), $"paradigm-mappers-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(output, "Mappers", "DataReaders"));
            var handwritten = Path.Combine(output, "Mappers", "DataReaders", "Handwritten.partial.cs");
            File.WriteAllText(handwritten, "// retained handwritten partial");
            var arguments = new List<string> { "generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--format", "json" };
            if (provider != "sqlserver")
                arguments.AddRange(["--database-provider", provider]);
            using var first = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync([.. arguments], first, TextWriter.Null), first.ToString());
            await MapperGenerationAssertions.AssertCompilesAndMapsAsync(assembly, output, provider);
            var snapshot = MapperGenerationAssertions.Snapshot(output);
            var manifest = Path.Combine(output, "Mappers", ".paradigm-mappers.json");
            var manifestBefore = File.ReadAllBytes(manifest);
            using var second = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync([.. arguments], second, TextWriter.Null), second.ToString());
            MapperGenerationAssertions.AssertSameFiles(snapshot, output);
            CollectionAssert.AreEqual(manifestBefore, File.ReadAllBytes(manifest));
            Assert.AreEqual("// retained handwritten partial", File.ReadAllText(handwritten));
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    [TestMethod]
    public async Task Mapper_generation_refuses_to_overwrite_handwritten_mapper_source()
    {
        var assembly = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var output = Path.Combine(Path.GetTempPath(), $"paradigm-mapper-overwrite-{Guid.NewGuid():N}");
        try
        {
            var parameters = Path.Combine(output, "Mappers", "SqlParameters");
            Directory.CreateDirectory(parameters);
            var handwritten = Path.Combine(parameters, "InputMapper.cs");
            File.WriteAllText(handwritten, "// handwritten mapper must survive");
            using var writer = new StringWriter();
            var exit = await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                writer, TextWriter.Null);

            Assert.AreEqual(1, exit);
            StringAssert.Contains(writer.ToString(), "PE8001");
            Assert.AreEqual("// handwritten mapper must survive", File.ReadAllText(handwritten));
            Assert.HasCount(1, Directory.EnumerateFiles(output, "*.cs", SearchOption.AllDirectories).ToArray());
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    [TestMethod]
    public async Task Mapper_generation_for_only_parameterless_routines_emits_a_compilable_registrar()
    {
        var reference = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-no-parameters-{Guid.NewGuid():N}");
        try
        {
            var assembly = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, "fixture"), """
                namespace FocusedFixture;
                public sealed class Refresh : Paradigm.Enterprise.Data.MySql.StoredProcedures.StoredProcedureBase
                {
                    protected override string StoredProcedureName => "Refresh";
                }
                """);
            var output = Path.Combine(temporary, "output");
            using var writer = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                writer, TextWriter.Null), writer.ToString());

            Assert.HasCount(1, MapperGenerationAssertions.Snapshot(output));
            var generated = MapperGenerationAssertions.CompileGenerated(reference, output);
            generated.GetType("MapperFixture.Data.Mappers.StoreProcedureMappersRegisterer", throwOnError: true)!
                .GetMethod("RegisterMappers")!.Invoke(null, null);
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("offset")]
    [DataRow("collection")]
    [DataRow("array-result")]
    [DataRow("nested-generic")]
    [DataRow("required-field")]
    public async Task Unsupported_mysql_shapes_fail_before_writing_any_generated_source(string shape)
    {
        var reference = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-unsupported-shape-{Guid.NewGuid():N}");
        try
        {
            var type = shape == "offset" ? "System.DateTimeOffset" : "System.Collections.Generic.List<int>";
            var source = shape switch
            {
                "array-result" => """
                namespace FocusedFixture;
                public sealed class Unsupported : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<object?, int[]>
                {
                    protected override string StoredProcedureName => "Unsupported";
                }
                """,
                "nested-generic" => """
                namespace FocusedFixture;
                public sealed class Outer<T> { public sealed class Row { public int Id { get; set; } } }
                public sealed class Unsupported : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<object?, Outer<int>.Row>
                {
                    protected override string StoredProcedureName => "Unsupported";
                }
                """,
                "required-field" => """
                namespace FocusedFixture;
                public sealed class Row { public required int Key; public int Value { get; set; } }
                public sealed class Unsupported : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<object?, Row>
                {
                    protected override string StoredProcedureName => "Unsupported";
                }
                """,
                _ => $$"""
                namespace FocusedFixture;
                public sealed class Input { public {{type}} Value { get; set; } = default!; }
                public sealed class Unsupported : Paradigm.Enterprise.Data.MySql.StoredProcedures.StoredProcedureBase<Input>
                {
                    protected override string StoredProcedureName => "Unsupported";
                }
                """
            };
            var assembly = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, "fixture"), source);
            var output = Path.Combine(temporary, "output");
            using var writer = new StringWriter();
            var exit = await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                writer, TextWriter.Null);

            Assert.AreEqual(1, exit, writer.ToString());
            StringAssert.Contains(writer.ToString(), "PE8001");
            Assert.IsFalse(Directory.Exists(output), "An unsupported shape must not create a partial mapper set.");
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public async Task Mapper_generation_escapes_keyword_result_type_names()
    {
        var reference = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-keyword-type-{Guid.NewGuid():N}");
        try
        {
            var assembly = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, "fixture"), """
                namespace FocusedFixture;
                public sealed class @event { public int Id { get; set; } }
                public sealed class Report : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<object?, @event>
                {
                    protected override string StoredProcedureName => "Report";
                }
                """);
            var output = Path.Combine(temporary, "output");
            using var writer = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                writer, TextWriter.Null), writer.ToString());

            MapperGenerationAssertions.CompileGenerated(assembly, output);
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public async Task Default_sqlserver_reader_preserves_property_defaults_for_missing_and_null_columns()
    {
        var reference = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-reader-defaults-{Guid.NewGuid():N}");
        try
        {
            var assembly = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, "fixture"), """
                namespace FocusedFixture;
                public sealed class Row
                {
                    public string Name { get; set; } = "fallback";
                    public int Value { get; set; }
                }
                public sealed class Report : Paradigm.Enterprise.Data.SqlServer.StoredProcedures.ResultStoredProcedureBase<object?, Row>
                {
                    protected override string StoredProcedureName => "Report";
                }
                """);
            var output = Path.Combine(temporary, "output");
            using var writer = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--format", "json"],
                writer, TextWriter.Null), writer.ToString());
            var generated = MapperGenerationAssertions.CompileGenerated(assembly, output);
            var mapperType = generated.GetType("MapperFixture.Data.Mappers.DataReaders.RowDataReaderMapper", throwOnError: true)!;
            foreach (var includeName in new[] { false, true })
            {
                var mapper = Activator.CreateInstance(mapperType)!;
                using var table = new System.Data.DataTable();
                if (includeName)
                {
                    table.Columns.Add("Name", typeof(string));
                    table.Rows.Add(DBNull.Value);
                }
                else
                {
                    table.Columns.Add("Value", typeof(int));
                    table.Rows.Add(7);
                }
                using var reader = table.CreateDataReader();
                Assert.IsTrue(reader.Read());
                var result = mapperType.GetMethod("Map")!.Invoke(mapper, [reader])!;
                Assert.AreEqual("fallback", result.GetType().GetProperty("Name")!.GetValue(result));
                if (!includeName)
                    Assert.AreEqual(7, result.GetType().GetProperty("Value")!.GetValue(result));
            }
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public async Task Required_nullable_init_result_maps_database_null_to_null()
    {
        var reference = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-nullable-init-{Guid.NewGuid():N}");
        try
        {
            var assembly = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, "fixture"), """
                namespace FocusedFixture;
                public sealed class Row { public required int? Count { get; init; } }
                public sealed class Report : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<object?, Row>
                {
                    protected override string StoredProcedureName => "Report";
                }
                """);
            var output = Path.Combine(temporary, "output");
            using var writer = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                writer, TextWriter.Null), writer.ToString());
            var generated = MapperGenerationAssertions.CompileGenerated(assembly, output);
            var mapperType = generated.GetType("MapperFixture.Data.Mappers.DataReaders.RowDataReaderMapper", throwOnError: true)!;
            var mapper = Activator.CreateInstance(mapperType)!;
            using var table = new System.Data.DataTable();
            table.Columns.Add("Count", typeof(int));
            table.Rows.Add(DBNull.Value);
            using var reader = table.CreateDataReader();
            Assert.IsTrue(reader.Read());
            var result = mapperType.GetMethod("Map")!.Invoke(mapper, [reader])!;
            Assert.IsNull(result.GetType().GetProperty("Count")!.GetValue(result));
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    public async Task Mapper_manifest_removes_only_stale_owned_files_when_contracts_change()
    {
        var reference = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-mapper-manifest-{Guid.NewGuid():N}");
        try
        {
            var output = Path.Combine(temporary, "output");
            var readers = Path.Combine(output, "Mappers", "DataReaders");
            Directory.CreateDirectory(readers);
            var handwritten = Path.Combine(readers, "BeforeRow.partial.cs");
            var untracked = Path.Combine(readers, "UserDataReaderMapper.cs");
            File.WriteAllText(handwritten, "// preserve handwritten partial");
            File.WriteAllText(untracked, "// preserve untracked mapper");
            foreach (var name in new[] { "BeforeRow", "AfterRow" })
            {
                var assembly = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, name), $$"""
                    namespace FocusedFixture;
                    public sealed class Input { public int Id { get; set; } }
                    public sealed class {{name}} { public int Id { get; set; } }
                    public sealed class Report : Paradigm.Enterprise.Data.MySql.StoredProcedures.ResultStoredProcedureBase<Input, {{name}}>
                    {
                        protected override string StoredProcedureName => "Report";
                    }
                    """);
                using var writer = new StringWriter();
                Assert.AreEqual(0, await TestCliApplication.RunAsync(
                    ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                    writer, TextWriter.Null), writer.ToString());
                Assert.IsTrue(File.Exists(Path.Combine(readers, name + "DataReaderMapper.cs")));
            }
            Assert.IsFalse(File.Exists(Path.Combine(readers, "BeforeRowDataReaderMapper.cs")));

            var parameterless = MapperGenerationAssertions.WriteFixture(reference, Path.Combine(temporary, "parameterless"), """
                namespace FocusedFixture;
                public sealed class Refresh : Paradigm.Enterprise.Data.MySql.StoredProcedures.StoredProcedureBase
                {
                    protected override string StoredProcedureName => "Refresh";
                }
                """);
            using var final = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", parameterless, "--output", output, "--database-provider", "mysql", "--format", "json"],
                final, TextWriter.Null), final.ToString());
            Assert.IsFalse(File.Exists(Path.Combine(readers, "AfterRowDataReaderMapper.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(output, "Mappers", "SqlParameters", "InputMapper.cs")));
            Assert.AreEqual("// preserve handwritten partial", File.ReadAllText(handwritten));
            Assert.AreEqual("// preserve untracked mapper", File.ReadAllText(untracked));
            MapperGenerationAssertions.CompileGenerated(reference, output);
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("../outside.cs")]
    [DataRow("/outside.cs")]
    [DataRow("DataReaders/../outside.cs")]
    [DataRow("DataReaders\\outside.cs")]
    [DataRow("C:/outside.cs")]
    public async Task Invalid_mapper_manifest_paths_are_rejected_before_any_write(string path)
    {
        var assembly = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var output = Path.Combine(Path.GetTempPath(), $"paradigm-invalid-manifest-{Guid.NewGuid():N}");
        try
        {
            var mappers = Path.Combine(output, "Mappers");
            Directory.CreateDirectory(mappers);
            var sentinel = Path.Combine(output, "outside.cs");
            File.WriteAllText(sentinel, "// outside mapper ownership");
            var manifest = Path.Combine(mappers, ".paradigm-mappers.json");
            var contents = System.Text.Json.JsonSerializer.Serialize(new { schemaVersion = "1.0", files = new[] { path } });
            File.WriteAllText(manifest, contents);
            var snapshot = MapperGenerationAssertions.Snapshot(output);
            using var writer = new StringWriter();
            var exit = await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", assembly, "--output", output, "--database-provider", "mysql", "--format", "json"],
                writer, TextWriter.Null);

            Assert.AreEqual(1, exit, writer.ToString());
            StringAssert.Contains(writer.ToString(), "PE8001");
            MapperGenerationAssertions.AssertSameFiles(snapshot, output);
            Assert.AreEqual(contents, File.ReadAllText(manifest));
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    #endregion

    #region Private Methods

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    #endregion
}