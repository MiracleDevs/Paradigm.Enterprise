using Paradigm.Enterprise.CodeGenerator;
using System.Reflection;

namespace Paradigm.Enterprise.Cli;

internal sealed class GenerateCommandHandler(string route, CodeGenerationMode mode) : CliCommandHandler<GenerateOptions>
{
    #region Fields

    private readonly CodeGenerationMode _mode = mode;

    #endregion

    #region Properties

    public override string Route { get; } = route;

    #endregion

    #region Overrides

    public override async Task<CommandResponse> ExecuteAsync(GenerateOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var result = await CodeGenerationService.GenerateAsync(new(_mode, options.ProjectName, options.AssemblyPath, options.OutputPath, options.Document, options.SettingsPath, options.DatabaseProvider), cancellationToken);
            return ResponseFactory.Create(Route, [], [new("generated-source", result.Mode, result.OutputPath)], []);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidOperationException or UnauthorizedAccessException or NotSupportedException or ReflectionTypeLoadException)
        {
            return ResponseFactory.Create(Route, [], [], [new("PE8001", "error", exception.Message, options.OutputPath)]);
        }
    }

    #endregion
}