using System.Data.Common;
using System.Reflection;

namespace Paradigm.Enterprise.Cli.Tests;

[TestClass]
[TestCategory("Integration")]
[TestCategory("OracleIntegration")]
[DoNotParallelize]
public sealed class OracleMapperIntegrationTests
{
    #region Public Methods

    [TestMethod]
    [Timeout(180_000)]
    public async Task Generated_oracle_mappers_round_trip_real_odp_parameters_and_result_types()
    {
        var connectionString = Environment.GetEnvironmentVariable("PARADIGM_ORACLE_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Set PARADIGM_ORACLE_TEST_CONNECTION_STRING to opt in to the Oracle integration fixture.");

        var fixture = MapperGenerationAssertions.FixtureAssembly(FindRepositoryRoot());
        var temporary = Path.Combine(Path.GetTempPath(), $"paradigm-oracle-mapper-{Guid.NewGuid():N}");
        try
        {
            using var writer = new StringWriter();
            Assert.AreEqual(0, await TestCliApplication.RunAsync(
                ["generate", "mappers", "--project-name", "MapperFixture", "--assembly", fixture, "--output", temporary, "--database-provider", "oracle", "--format", "json"],
                writer, TextWriter.Null), writer.ToString());
            var generated = MapperGenerationAssertions.CompileGenerated(fixture, temporary);
            var inputType = Assembly.LoadFrom(fixture).GetType("MapperFixture.OracleInput", throwOnError: true)!;
            var input = Activator.CreateInstance(inputType)!;
            var token = Guid.NewGuid();
            var offset = new DateTimeOffset(2026, 11, 14, 12, 30, 0, TimeSpan.FromHours(5.5));
            var duration = TimeSpan.FromHours(25) + TimeSpan.FromTicks(123_456);
            var occurredAt = new DateTime(2026, 11, 14, 10, 15, 30);
            var payload = new byte[] { 0, 128, 255 };
            foreach (var (name, value) in new (string, object)[]
            {
                ("CategoryId", 42), ("Token", token), ("Enabled", true), ("Amount", 12.3456m),
                ("SinceUtc", occurredAt), ("Offset", offset), ("Duration", duration),
                ("Payload", payload), ("Letter", 'Ω')
            })
                inputType.GetProperty(name)!.SetValue(input, value);

            var provider = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(fixture)!, "Oracle.ManagedDataAccess.dll"));
            await using var connection = (DbConnection)Activator.CreateInstance(provider.GetType("Oracle.ManagedDataAccess.Client.OracleConnection", throwOnError: true)!, [connectionString])!;
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.GetType().GetProperty("BindByName")!.SetValue(command, true);
            command.CommandTimeout = 30;
            command.CommandText = """
                SELECT :CategoryId AS "Id", :Token AS "Token", :Name AS "Name", :Amount AS "Amount",
                       :SinceUtc AS "OccurredAt", :Enabled AS "Enabled", :OptionalEnabled AS "OptionalEnabled",
                       :OptionalToken AS "OptionalToken", :Offset AS "Offset", :Duration AS "Duration",
                       :Payload AS "Payload", :Letter AS "Letter", :OptionalLetter AS "OptionalLetter"
                FROM dual
                """;
            using var mapper = (IDisposable)Activator.CreateInstance(generated.GetType("MapperFixture.Data.Mappers.SqlParameters.OracleInputMapper", throwOnError: true)!)!;
            var parameters = ((Array)mapper.GetType().GetMethod("Map")!.Invoke(mapper, [input])!).Cast<DbParameter>().ToArray();
            command.Parameters.AddRange(parameters);
            await using (var reader = await command.ExecuteReaderAsync())
            {
                Assert.IsTrue(await reader.ReadAsync());
                var row = Map(generated, "ReportRow", reader);
                Assert.AreEqual(42, Property(row, "Id"));
                Assert.AreEqual(token, Property(row, "Token"));
                Assert.AreEqual(true, Property(row, "Enabled"));
                Assert.AreEqual(12.3456m, Property(row, "Amount"));
                Assert.AreEqual(occurredAt, Property(row, "OccurredAt"));
                Assert.IsNull(Property(row, "Name"));
                CollectionAssert.AreEqual(payload, (byte[])Property(row, "Payload")!);

                var types = Map(generated, "OracleTypesRow", reader);
                Assert.AreEqual(offset, Property(types, "Offset"));
                Assert.AreEqual(offset.Offset, ((DateTimeOffset)Property(types, "Offset")!).Offset);
                Assert.AreEqual(duration, Property(types, "Duration"));
                Assert.AreEqual('Ω', Property(types, "Letter"));
                Assert.IsNull(Property(types, "OptionalLetter"));
                Assert.IsNull(Property(types, "OptionalToken"));
                Assert.IsNull(Property(types, "OptionalEnabled"));
            }
            await using (var nativeBoolean = connection.CreateCommand())
            {
                nativeBoolean.CommandText = "SELECT TRUE AS \"Enabled\" FROM dual";
                await using var reader = await nativeBoolean.ExecuteReaderAsync();
                Assert.IsTrue(await reader.ReadAsync());
                Assert.AreEqual(true, Property(Map(generated, "ReportRow", reader), "Enabled"));
            }
            await using var reuse = connection.CreateCommand();
            reuse.CommandText = "SELECT 1 FROM dual";
            Assert.AreEqual(1, Convert.ToInt32(await reuse.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));
        }
        finally
        {
            if (Directory.Exists(temporary))
                Directory.Delete(temporary, recursive: true);
        }
    }

    #endregion

    #region Private Methods

    private static object Map(Assembly generated, string name, DbDataReader reader)
    {
        var mapper = Activator.CreateInstance(generated.GetType("MapperFixture.Data.Mappers.DataReaders." + name + "DataReaderMapper", throwOnError: true)!)!;
        return mapper.GetType().GetMethod("Map")!.Invoke(mapper, [reader])!;
    }

    private static object? Property(object row, string name) => row.GetType().GetProperty(name)!.GetValue(row);

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "src", "Paradigm.Enterprise.slnx")))
                return current.FullName;
        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    #endregion
}
