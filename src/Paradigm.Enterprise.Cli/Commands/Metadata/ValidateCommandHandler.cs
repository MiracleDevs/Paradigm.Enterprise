namespace Paradigm.Enterprise.Cli;
internal sealed class ValidateCommandHandler(MetadataCommandLoader loader, IValidationService validation, IConfigurationService configuration) : CliCommandHandler<ValidateOptions>
{
#region Properties
    public override string Route => "validate";

#endregion
#region Overrides
    public override Task<CommandResponse> ExecuteAsync(ValidateOptions options, CancellationToken cancellationToken)
    {
        var loaded = loader.Load(options.Project, options.Framework, true, false);
        var application = loaded.Types.Where(x => !x.AssemblyName.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
        var results = validation.Inspect(loaded.Types).Where(x => x.Project is not null && !x.Project.StartsWith("Paradigm.Enterprise", StringComparison.Ordinal)).ToArray();
        var configured = configuration.Load(loaded.Selection, null);
        var policyDiagnostics = validation.ValidateTypes(application, loaded.Types).Where(diagnostic => !SuppressionPolicy.IsSuppressed(diagnostic, configured.Configuration.Suppressions));
        var diagnostics = loaded.Diagnostics.Concat(validation.ValidateLayers(loaded.Selection)).Concat(configured.Diagnostics).Concat(SuppressionPolicy.Expired(configured.Configuration.Suppressions, configured.Configuration.Path)).Concat(policyDiagnostics);
        return Task.FromResult(ResponseFactory.Create(Route, loaded.Packages, results, diagnostics));
    }
#endregion
}
