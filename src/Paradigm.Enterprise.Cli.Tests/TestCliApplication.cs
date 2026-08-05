namespace Paradigm.Enterprise.Cli.Tests;

internal static class TestCliApplication
{
    #region Public Methods

    public static Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken = default)
    {
        IProjectResolutionService projects = new ProjectResolutionService();
        IAssetService assets = new AssetService();
        IMetadataService metadata = new MetadataService();
        IApiQueryService api = new ApiQueryService();
        IValidationService validation = new ValidationService();
        IResponseWriter responseWriter = new DefaultResponseWriter();
        IExitPolicy exitPolicy = new DefaultExitPolicy();
        var metadataLoader = new MetadataCommandLoader(projects, assets, metadata);
        var configuration = new ConfigurationService();
        var handlers = new ICliCommandHandler[]
        {
            new HelpCommandHandler(),
            new VersionCommandHandler(),
            new DoctorCommandHandler(projects, assets),
            new ApiSearchCommandHandler(metadataLoader, api),
            new ApiShowCommandHandler(metadataLoader, api),
            new ApiGuideCommandHandler(metadataLoader, api, new ApiGuideService()),
            new InspectCommandHandler(metadataLoader, validation),
            new ValidateCommandHandler(metadataLoader, validation, configuration),
            new ChecksListCommandHandler(),
            new ChecksRunCommandHandler(projects, assets, configuration),
            new GenerateCommandHandler("generate json", Paradigm.Enterprise.CodeGenerator.CodeGenerationMode.Json),
            new GenerateCommandHandler("generate mappers", Paradigm.Enterprise.CodeGenerator.CodeGenerationMode.Mappers),
            new GenerateCommandHandler("generate client", Paradigm.Enterprise.CodeGenerator.CodeGenerationMode.Client),
            new ScaffoldSolutionCommandHandler(new SolutionScaffolder()),
            new DatabaseValidateCommandHandler(new DatabaseProjectValidator()),
            new PackagesCheckCommandHandler(projects, assets, configuration, new PackagePolicyService()),
            new PackagesAuditCommandHandler(projects, assets, configuration, new PackagePolicyService(), new DotNetPackageAuditor())
        };
        var application = new CliApplication(new CommandRouter(handlers), responseWriter, exitPolicy, output, error);
        return application.RunAsync(args, cancellationToken);
    }

    #endregion
}