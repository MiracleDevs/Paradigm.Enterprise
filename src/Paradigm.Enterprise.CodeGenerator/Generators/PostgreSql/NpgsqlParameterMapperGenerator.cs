using Microsoft.Extensions.Logging;

namespace Paradigm.Enterprise.CodeGenerator.Generators.PostgreSql;
internal class NpgsqlParameterMapperGenerator
{
    private readonly ILogger _logger;
    private readonly string _mappersOutputPath;
    private readonly string _projectName;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlParameterMapperGenerator"/> class.
    /// </summary>
    /// <param name="mappersOutputPath">The mappers output path.</param>
    /// <param name="projectName">Name of the project.</param>
    /// <param name="logger">The logger.</param>
    public NpgsqlParameterMapperGenerator(string mappersOutputPath, string? projectName, ILogger logger)
    {
        _logger = logger;
        _projectName = projectName ?? string.Empty;
        _mappersOutputPath = mappersOutputPath;
    }

    /// <summary>
    /// Generates the mapper code.
    /// </summary>
    /// <param name="genericArguments">The generic arguments.</param>
    /// <param name="generatedTypes">The generated types.</param>
    public void GenerateMapperCode(Type[] genericArguments, List<string> generatedTypes)
    {
        throw new NotImplementedException();
    }
}
