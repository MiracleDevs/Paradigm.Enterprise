using Paradigm.Enterprise.Cli;
using Paradigm.Enterprise.CodeGenerator;

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
    new GenerateCommandHandler("generate json", CodeGenerationMode.Json),
    new GenerateCommandHandler("generate mappers", CodeGenerationMode.Mappers),
    new GenerateCommandHandler("generate client", CodeGenerationMode.Client),
    new PackagesCheckCommandHandler(projects, assets, configuration, new PackagePolicyService()),
    new PackagesAuditCommandHandler(
        projects, assets, configuration, new PackagePolicyService(), new DotNetPackageAuditor())
};
var application = new CliApplication(
    new CommandRouter(handlers), responseWriter, exitPolicy, Console.Out, Console.Error);
return await application.RunAsync(args, CancellationToken.None);