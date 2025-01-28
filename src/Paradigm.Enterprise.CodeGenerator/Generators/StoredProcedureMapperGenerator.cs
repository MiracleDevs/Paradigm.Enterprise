using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace Paradigm.Enterprise.CodeGenerator.Generators;

internal class StoredProcedureMapperGenerator
{
    #region Properties

    /// <summary>
    /// Gets or sets the output path.
    /// </summary>
    /// <value>
    /// The output path.
    /// </value>
    private string OutputPath { get; set; }

    /// <summary>
    /// The data assembly path
    /// </summary>
    private readonly string? _dataAssemblyPath;

    /// <summary>
    /// The project name
    /// </summary>
    private readonly string? _projectName;

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
        OutputPath = string.Empty;
        _dataAssemblyPath = configuration.GetValue<string>("DataAssemblyPath");
        _projectName = configuration.GetValue<string>("ProjectName");
        _logger = logger;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates the code.
    /// </summary>
    public void GenerateCode()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_dataAssemblyPath))
                throw new ArgumentNullException("DataAssemblyPath");

            if (string.IsNullOrWhiteSpace(_projectName))
                throw new ArgumentNullException("ProjectName");

            OutputPath = Path.Combine(_dataAssemblyPath, "Mappers");

            var storedProcedureTypes = Assembly.LoadFrom(_dataAssemblyPath).GetTypes().Where(IsStoredProcedureClass);
            var dataReaderMappers = GenerateDataReaderMappers(storedProcedureTypes);
            var sqlParameterMappers = GenerateSqlParameterMappers(storedProcedureTypes);

            GenerateMappersRegisterer(dataReaderMappers, sqlParameterMappers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        finally
        {
            _logger.LogInformation("Finished DataReader mappers generation.");
            _logger.LogInformation("---------------------------------------------");
        }
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

    /// <summary>
    /// Generates the data reader mappers.
    /// </summary>
    /// <param name="storedProcedureTypes">The stored procedure types.</param>
    /// <returns></returns>
    private List<string> GenerateDataReaderMappers(IEnumerable<Type> storedProcedureTypes)
    {
        _logger.LogInformation("Starting DataReader mappers generation.");

        var mappersOutputPath = Path.Combine(OutputPath, "DataReaders");

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
                        if (genericArgument.GetGenericTypeDefinition() == typeof(Nullable<>))
                            targetType = Nullable.GetUnderlyingType(genericArgument);
                        else
                            targetType = genericArgument.GetGenericArguments().First();

                    if (string.IsNullOrWhiteSpace(targetType?.FullName) || generatedTypes.Contains(targetType.FullName))
                        continue;

                    var mapperClassName = $"{targetType.Name}DataReaderMapper";
                    var propertyAssignments = new StringBuilder();

                    foreach (var property in targetType.GetProperties())
                        GenerateDataReaderMapperPropertyAssignments(propertyAssignments, property, targetType.Name);

                    var sourceCode = $@"// <auto-generated/>
using {_projectName}.Data.Core.StoredProcedures.Mappers;
using {targetType.Namespace};
using System.Data;

namespace {_projectName}.Data.Mappers.DataReaders;

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
            if (propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                propertyType = Nullable.GetUnderlyingType(propertyType);
            else
                return;

        if (propertyType is null ||
            propertyType.Name.EndsWith("view", StringComparison.OrdinalIgnoreCase) ||
            propertyType.Name.EndsWith("dto", StringComparison.OrdinalIgnoreCase))
            return;

        var getValueMethod = propertyType switch
        {
            Type t when t == typeof(int) => "GetInt32",
            Type t when t == typeof(string) => "GetString",
            Type t when t == typeof(DateTime) => "GetDateTime",
            Type t when t == typeof(DateTimeOffset) => "GetDateTimeOffset",
            Type t when t == typeof(bool) => "GetBoolean",
            Type t when t == typeof(double) => "GetDouble",
            Type t when t == typeof(float) => "GetFloat",
            Type t when t == typeof(long) => "GetInt64",
            Type t when t == typeof(short) => "GetInt16",
            Type t when t == typeof(char) => "GetChar",
            Type t when t == typeof(decimal) => "GetDecimal",
            Type t when t == typeof(byte) => "GetByte",
            Type t when t == typeof(byte[]) => "GetBytes",
            _ => null
        };

        if (getValueMethod is null)
            throw new InvalidOperationException($"Couldn't resolve property assignment for type {propertyType.Name}");

        var propertyAssignment = $"instance.{propertyName} = {getValueMethod}(reader, nameof({targetTypeName}.{propertyName}));";
        propertyAssignments.AppendLine($"        if (FieldIsValid(reader, nameof({targetTypeName}.{propertyName}))) {propertyAssignment}");
    }

    /// <summary>
    /// Generates the SQL parameter mappers.
    /// </summary>
    /// <param name="storedProcedureTypes">The stored procedure types.</param>
    /// <returns></returns>
    private List<string> GenerateSqlParameterMappers(IEnumerable<Type> storedProcedureTypes)
    {
        _logger.LogInformation("Starting SqlParameter mappers generation.");

        var mappersOutputPath = Path.Combine(OutputPath, "SqlParameters");

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

                    foreach (var property in targetType.GetProperties())
                        GenerateSqlParameterMapperPropertyAssignments(propertyAssignments, property, targetType.Name);

                    var sourceCode = $@"// <auto-generated/>
using {_projectName}.Data.Core.StoredProcedures.Mappers;
using {targetType.Namespace};

namespace {_projectName}.Data.Mappers.SqlParameters;

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
                    propertyAssignments.AppendLine($"                dataRow[\"{itemProperty.Name}\"] = item.{itemProperty.Name};");

                propertyAssignments.AppendLine($"                table{propertyName}.Rows.Add(dataRow);");
                propertyAssignments.AppendLine("            }");
            }
            else
            {
                propertyAssignments.AppendLine(string.Empty);
                propertyAssignments.AppendLine($"            var dataRow = table{propertyName}.NewRow();");

                foreach (var itemProperty in itemProperties)
                    propertyAssignments.AppendLine($"            dataRow[\"{itemProperty.Name}\"] = instance.{propertyName}.{itemProperty.Name};");

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
    /// <param name="dataReaderMappers">The data reader mappers.</param>
    /// <param name="sqlParameterMappers">The SQL parameter mappers.</param>
    private void GenerateMappersRegisterer(List<string> dataReaderMappers, List<string> sqlParameterMappers)
    {
        var registerDataReaderMappers = new StringBuilder();
        var registerSqlParameterMappers = new StringBuilder();

        foreach (var generatedType in dataReaderMappers)
        {
            var className = generatedType.Substring(generatedType.LastIndexOf('.') + 1);
            registerDataReaderMappers.AppendLine($"        DataReaderMapperFactory.RegisterMapper<{generatedType.Replace($"{_projectName}.", string.Empty)}>(new {className}DataReaderMapper());");
        }


        foreach (var generatedType in sqlParameterMappers)
        {
            var className = generatedType.Substring(generatedType.LastIndexOf('.') + 1);
            registerSqlParameterMappers.AppendLine($"        SqlParameterMapperFactory.RegisterMapper<{generatedType.Replace($"{_projectName}.", string.Empty)}>(new {className}Mapper());");
        }

        var sourceCode = $@"// <auto-generated/>
using {_projectName}.Data.Core.StoredProcedures;
using {_projectName}.Data.Mappers.DataReaders;
using {_projectName}.Data.Mappers.SqlParameters;

namespace {_projectName}.Data.Mappers;

public static class StoreProcedureMappersRegisterer
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

        File.WriteAllText(Path.Combine(OutputPath, "StoreProcedureMappersRegisterer.cs"), sourceCode);
        _logger.LogInformation($"Generated StoreProcedureMappersRegisterer class with {dataReaderMappers.Count} DataReaderMappers and {sqlParameterMappers.Count} SqlParameterMappers.");
    }

    #endregion
}