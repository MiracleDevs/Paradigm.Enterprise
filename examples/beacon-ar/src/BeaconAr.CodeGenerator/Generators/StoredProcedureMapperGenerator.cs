using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BeaconAr.CodeGenerator.Extensions;
using BeaconAr.Data.Access.Context;

using System.Reflection;
using System.Text;

namespace BeaconAr.CodeGenerator.Generators;

internal class StoredProcedureMapperGenerator
{
    #region Properties

    /// <summary>
    /// The configuration
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedureMapperGenerator" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public StoredProcedureMapperGenerator(IConfiguration configuration, ILogger<StoredProcedureMapperGenerator> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates the code.
    /// </summary>
    public void GenerateCode()
    {
        var outputPath = _configuration.GetRequiredSection("dataReaderMapperGenerator").GetValue<string?>("outputPath");
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        var storedProcedureTypes = typeof(AccessDbContext).Assembly.GetTypes()
            .Where(IsStoredProcedureClass)
            .OrderBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal)
            .ToArray();
        foreach (IGrouping<string, Type> capabilityGroup in storedProcedureTypes
                     .GroupBy(GetCapability, StringComparer.Ordinal)
                     .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            using var output = new AtomicOutputDirectory(Path.Combine(outputPath, capabilityGroup.Key, "Mappers"));
            Type[] capabilityTypes = capabilityGroup.OrderBy(static type => type.FullName ?? type.Name, StringComparer.Ordinal).ToArray();
            var dataReaderMappers = GenerateDataReaderMappers(capabilityTypes, output.Path, capabilityGroup.Key);
            var sqlParameterMappers = GenerateSqlParameterMappers(capabilityTypes, output.Path, capabilityGroup.Key);
            GenerateMappersRegisterer(output.Path, capabilityGroup.Key, dataReaderMappers, sqlParameterMappers);
            output.Commit();
        }
        _logger.LogInformation("Finished DataReader mappers generation.");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Determines whether [is stored procedure class] [the specified type].
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>
    ///   <c>true</c> if [is stored procedure class] [the specified type]; otherwise, <c>false</c>.
    /// </returns>
    private bool IsStoredProcedureClass(Type type) => type.IsClass && !type.IsAbstract && type.BaseType is not null &&
        type.BaseType.Name.Contains("StoredProcedureBase");

    private static string GetCapability(Type type)
    {
        const string prefix = "BeaconAr.Data.";
        const string suffix = ".StoredProcedures";
        string typeNamespace = type.Namespace ?? throw new InvalidOperationException($"Stored procedure type '{type.Name}' has no namespace.");
        if (!typeNamespace.StartsWith(prefix, StringComparison.Ordinal) || !typeNamespace.EndsWith(suffix, StringComparison.Ordinal))
            throw new InvalidOperationException($"Stored procedure type '{type.FullName}' is not capability-owned.");
        return typeNamespace.Substring(prefix.Length, typeNamespace.Length - prefix.Length - suffix.Length);
    }

    /// <summary>
    /// Generates the data reader mappers.
    /// </summary>
    /// <param name="storedProcedureTypes">The stored procedure types.</param>
    /// <param name="outputPath">The output path.</param>
    private List<string> GenerateDataReaderMappers(IEnumerable<Type> storedProcedureTypes, string outputPath, string capability)
    {
        _logger.LogInformation("Starting DataReader mappers generation.");

        var mappersOutputPath = Path.Combine(outputPath, "DataReaders");

        if (!Directory.Exists(mappersOutputPath))
        {
            var directory = Directory.CreateDirectory(mappersOutputPath);
            _logger.LogInformation($"Created directory '{directory.FullName}'.");
        }

        var generatedTypes = new List<string>();

        foreach (var storedProcedure in storedProcedureTypes)
        {
            var baseType = storedProcedure.BaseType;
            if (baseType is not null && baseType.IsGenericType)
            {
                var genericArguments = baseType.GetGenericArguments()?
                    .Where(x => x.IsClass && !x.Name.EndsWith("parameters", StringComparison.OrdinalIgnoreCase)).ToArray();

                if (genericArguments is null) continue;

                foreach (var genericArgument in genericArguments)
                {
                    var targetType = genericArgument;
                    if (genericArgument.IsGenericType)
                    {
                        if (genericArgument.GetGenericTypeDefinition() == typeof(Nullable<>))
                            targetType = Nullable.GetUnderlyingType(genericArgument);
                        else
                            targetType = genericArgument.GetGenericArguments().First();
                    }

                    if (string.IsNullOrWhiteSpace(targetType?.FullName) || generatedTypes.Contains(targetType.FullName))
                        continue;

                    var mapperClassName = $"{targetType.Name}DataReaderMapper";
                    var propertyAssignments = new StringBuilder();

                    foreach (var property in targetType.GetProperties().OrderBy(static property => property.MetadataToken))
                    {
                        GenerateDataReaderMapperPropertyAssignments(propertyAssignments, property, targetType.Name);
                    }

                    var sourceCode = $@"// <auto-generated/>
using {targetType.Namespace};
using Paradigm.Enterprise.Data.StoredProcedures.Mappers;
using System.Data;

namespace BeaconAr.Data.{capability}.Mappers.DataReaders;

internal partial class {mapperClassName} : DataReaderMapperBase
{{
    public override object Map(IDataReader reader)
    {{
        LoadReaderFields(reader);

        var instance = new {targetType.Name}();
{propertyAssignments}
        return instance;
    }}
}}";

                    var fileName = $"{mapperClassName}.cs";
                    File.WriteAllText(Path.Combine(mappersOutputPath, fileName), sourceCode);
                    generatedTypes.Add(targetType.FullName);
                    _logger.LogInformation($"Generated '{fileName}'.");
                }
            }
        }

        return generatedTypes;
    }

    /// <summary>
    /// Generates the data reader mapper property assignments.
    /// </summary>
    /// <param name="propertyAssignments">The property assignments.</param>
    /// <param name="property">The property.</param>
    /// <param name="targetTypeName">Name of the target type.</param>
    /// <exception cref="InvalidOperationException">Couldn't resolve property assignment for type {propertyType.Name}</exception>
    private void GenerateDataReaderMapperPropertyAssignments(StringBuilder propertyAssignments, PropertyInfo property, string targetTypeName)
    {
        var propertyName = property.Name;
        var propertyType = property.PropertyType;

        if (propertyType.IsGenericType)
        {
            if (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                propertyType = Nullable.GetUnderlyingType(propertyType);
            else
                return;
        }

        if (propertyType is null ||
            propertyType.Name.EndsWith("view", StringComparison.OrdinalIgnoreCase) ||
            propertyType.Name.EndsWith("dto", StringComparison.OrdinalIgnoreCase))
            return;

        var getValueMethod = propertyType switch
        {
            var t when t == typeof(int) => "GetInt32",
            var t when t == typeof(string) => "GetString",
            var t when t == typeof(DateTime) => "GetDateTime",
            var t when t == typeof(DateTimeOffset) => "GetDateTimeOffset",
            var t when t == typeof(bool) => "GetBoolean",
            var t when t == typeof(double) => "GetDouble",
            var t when t == typeof(float) => "GetFloat",
            var t when t == typeof(long) => "GetInt64",
            var t when t == typeof(short) => "GetInt16",
            var t when t == typeof(char) => "GetChar",
            var t when t == typeof(decimal) => "GetDecimal",
            var t when t == typeof(byte) => "GetByte",
            var t when t == typeof(byte[]) => "GetBytes",
            var t when t == typeof(DateOnly) => "GetDateTime",
            var t when t == typeof(string[]) => "GetArray<string>",
            var t when t == typeof(int[]) => "GetArray<int>",
            _ => null
        };

        if (getValueMethod is null)
            throw new InvalidOperationException($"Couldn't resolve property assignment for type {propertyType.Name}");

        var mappedValue = propertyType == typeof(DateOnly)
            ? $"DateOnly.FromDateTime({getValueMethod}(reader, nameof({targetTypeName}.{propertyName})))"
            : $"{getValueMethod}(reader, nameof({targetTypeName}.{propertyName}))";
        var propertyAssignment = $"instance.{propertyName} = {mappedValue};";
        propertyAssignments.AppendLine($"        if (FieldIsValid(reader, nameof({targetTypeName}.{propertyName}))) {propertyAssignment}");
    }

    /// <summary>
    /// Generates the SQL parameter mappers.
    /// </summary>
    /// <param name="storedProcedureTypes">The stored procedure types.</param>
    /// <param name="outputPath">The output path.</param>
    private List<string> GenerateSqlParameterMappers(IEnumerable<Type> storedProcedureTypes, string outputPath, string capability)
    {
        _logger.LogInformation("Starting SqlParameter mappers generation.");

        var mappersOutputPath = Path.Combine(outputPath, "SqlParameters");

        if (!Directory.Exists(mappersOutputPath))
        {
            var directory = Directory.CreateDirectory(mappersOutputPath);
            _logger.LogInformation($"Created directory '{directory.FullName}'.");
        }

        var generatedTypes = new List<string>();

        foreach (var storedProcedure in storedProcedureTypes)
        {
            var baseType = storedProcedure.BaseType;
            if (baseType is not null && baseType.IsGenericType)
            {
                var genericArguments = baseType.GetGenericArguments()?
                    .Where(x => x.IsClass && x.Name.EndsWith("parameters", StringComparison.OrdinalIgnoreCase)).ToArray();

                if (genericArguments is null) continue;

                foreach (var genericArgument in genericArguments)
                {
                    var targetType = Nullable.GetUnderlyingType(genericArgument) ?? genericArgument;

                    if (string.IsNullOrWhiteSpace(targetType?.FullName) || generatedTypes.Contains(targetType.FullName))
                        continue;

                    var mapperClassName = $"{targetType.Name}Mapper";
                    var propertyAssignments = new StringBuilder();

                    foreach (var property in targetType.GetProperties().OrderBy(static property => property.MetadataToken))
                    {
                        GenerateSqlParameterMapperPropertyAssignments(propertyAssignments, property, targetType.Name);
                    }

                    var sourceCode = $@"// <auto-generated/>
using Paradigm.Enterprise.Data.SqlServer.StoredProcedures.Mappers;
using {targetType.Namespace};

namespace BeaconAr.Data.{capability}.Mappers.SqlParameters;

internal partial class {mapperClassName} : SqlParameterMapperBase
{{
    protected override void AddSqlParameters(object parameters)
    {{
        if (!(parameters is {targetType.Name} instance)) return;

{propertyAssignments}
    }}
}}";

                    var fileName = $"{mapperClassName}.cs";
                    File.WriteAllText(Path.Combine(mappersOutputPath, fileName), sourceCode);
                    generatedTypes.Add(targetType.FullName);
                    _logger.LogInformation($"Generated '{fileName}'.");
                }
            }
        }

        return generatedTypes;
    }

    /// <summary>
    /// Generates the SQL parameter mapper property assignments.
    /// </summary>
    /// <param name="propertyAssignments">The property assignments.</param>
    /// <param name="property">The property.</param>
    /// <param name="targetTypeName">Name of the target type.</param>
    /// <exception cref="InvalidOperationException">Couldn't resolve property assignment for type {propertyType.Name}</exception>
    private void GenerateSqlParameterMapperPropertyAssignments(StringBuilder propertyAssignments, PropertyInfo property, string targetTypeName)
    {
        var propertyName = property.Name;
        var propertyType = property.PropertyType;

        var generateDataTableParameter = false;
        var generateSingleRowDataTableParameter = false;

        if (propertyType.IsGenericType)
        {
            var genericType = propertyType.GetGenericTypeDefinition();
            if (genericType == typeof(IEnumerable<>))
            {
                propertyType = propertyType.GetGenericArguments().FirstOrDefault();
                generateDataTableParameter = true;
            }
            else if (genericType == typeof(Nullable<>))
                propertyType = Nullable.GetUnderlyingType(propertyType);
            else
                return;
        }
        else if (propertyType.IsClass && !"System".Equals(propertyType.Namespace))
            generateSingleRowDataTableParameter = true;

        if (propertyType is null)
            return;

        if (generateDataTableParameter || generateSingleRowDataTableParameter)
        {
            var itemProperties = propertyType.GetProperties();
            if (itemProperties.Length == 0) return;

            propertyAssignments.AppendLine(string.Empty);
            propertyAssignments.AppendLine($"        if (instance.{propertyName} is not null)");
            propertyAssignments.AppendLine("        {");
            propertyAssignments.AppendLine($"            var table{propertyName} = new System.Data.DataTable();");

            foreach (var itemProperty in itemProperties)
            {
                var itemPropertyType = Nullable.GetUnderlyingType(itemProperty.PropertyType) ?? itemProperty.PropertyType;
                propertyAssignments.AppendLine($"            table{propertyName}.Columns.Add(\"{itemProperty.Name}\", typeof({itemPropertyType.FullName}));");
            }

            if (generateDataTableParameter)
            {
                propertyAssignments.AppendLine($"            foreach (var item in instance.{propertyName})");
                propertyAssignments.AppendLine("            {");
                propertyAssignments.AppendLine($"                var dataRow = table{propertyName}.NewRow();");

                foreach (var itemProperty in itemProperties)
                {
                    var valueSetter = itemProperty.IsNullable()
                        ? $"item.{itemProperty.Name}.HasValue ? item.{itemProperty.Name}.Value : DBNull.Value"
                        : $"item.{itemProperty.Name}";

                    propertyAssignments.AppendLine($"                dataRow[\"{itemProperty.Name}\"] = {valueSetter};");
                }

                propertyAssignments.AppendLine($"                table{propertyName}.Rows.Add(dataRow);");
                propertyAssignments.AppendLine("            }");
            }
            else
            {
                propertyAssignments.AppendLine(string.Empty);
                propertyAssignments.AppendLine($"            var dataRow = table{propertyName}.NewRow();");

                foreach (var itemProperty in itemProperties)
                {
                    var valueSetter = itemProperty.IsNullable()
                        ? $"instance.{propertyName}.{itemProperty.Name}.HasValue ? instance.{propertyName}.{itemProperty.Name}.Value : DBNull.Value"
                        : $"instance.{propertyName}.{itemProperty.Name}";

                    propertyAssignments.AppendLine($"            dataRow[\"{itemProperty.Name}\"] = {valueSetter};");
                }

                propertyAssignments.AppendLine($"            table{propertyName}.Rows.Add(dataRow);");
            }

            propertyAssignments.AppendLine($"            AddSqlParameter(nameof({targetTypeName}.{propertyName}), table{propertyName});");
            propertyAssignments.AppendLine("        }");
        }
        else
            propertyAssignments.AppendLine($"        AddSqlParameter(nameof({targetTypeName}.{propertyName}), instance.{propertyName});");
    }

    /// <summary>
    /// Generates the mappers registerer.
    /// </summary>
    /// <param name="outputPath">The output path.</param>
    /// <param name="generatedTypes">The generated types.</param>
    private void GenerateMappersRegisterer(string outputPath, string capability, List<string> dataReaderMappers, List<string> sqlParameterMappers)
    {
        var registerDataReaderMappers = new StringBuilder();
        var registerSqlParameterMappers = new StringBuilder();

        foreach (var generatedType in dataReaderMappers.OrderBy(static type => type, StringComparer.Ordinal))
        {
            var className = generatedType.Substring(generatedType.LastIndexOf('.') + 1);
            registerDataReaderMappers.AppendLine($"        DataReaderMapperFactory.RegisterMapper<{generatedType.Replace("BeaconAr.", string.Empty)}>(() => new DataReaders.{className}DataReaderMapper());");
        }

        foreach (var generatedType in sqlParameterMappers.OrderBy(static type => type, StringComparer.Ordinal))
        {
            var className = generatedType.Substring(generatedType.LastIndexOf('.') + 1);
            registerSqlParameterMappers.AppendLine($"        SqlParameterMapperFactory.RegisterMapper<{generatedType.Replace("BeaconAr.", string.Empty)}>(() => new SqlParameters.{className}Mapper());");
        }

        var sourceCode = $@"// <auto-generated/>
using Paradigm.Enterprise.Data.SqlServer.StoredProcedures;
using Paradigm.Enterprise.Data.StoredProcedures.Mappers;

namespace BeaconAr.Data.{capability}.Mappers;

public static class {capability}StoredProcedureMappersRegisterer
{{
    public static void RegisterMappers()
    {{
        RegisterDataReaderMappers();
        RegisterSqlParameterMappers();
    }}

    private static void RegisterDataReaderMappers()
    {{
{registerDataReaderMappers}
    }}

    private static void RegisterSqlParameterMappers()
    {{
{registerSqlParameterMappers}
    }}
}}";

        File.WriteAllText(Path.Combine(outputPath, $"{capability}StoredProcedureMappersRegisterer.cs"), sourceCode);
        _logger.LogInformation($"Generated {capability}StoredProcedureMappersRegisterer class with {dataReaderMappers.Count} DataReaderMappers and {sqlParameterMappers.Count} SqlParameterMappers.");
    }

    #endregion
}
