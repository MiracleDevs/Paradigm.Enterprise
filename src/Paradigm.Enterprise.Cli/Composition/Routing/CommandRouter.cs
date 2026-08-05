namespace Paradigm.Enterprise.Cli;

internal sealed class CommandRouter
{
    #region Fields

    private readonly IReadOnlyDictionary<string, ICliCommandHandler> handlers;

    #endregion

    #region Constructors

    public CommandRouter(IEnumerable<ICliCommandHandler> handlers)
    {
        this.handlers = handlers.ToDictionary(x => x.Route, StringComparer.Ordinal);
    }

    #endregion

    #region Public Methods

    public Task<CommandResponse> RouteAsync(ParsedCommand command, CancellationToken cancellationToken) => handlers.TryGetValue(command.Name, out var handler) ? handler.ExecuteAsync(command.Options, cancellationToken) : throw new ArgumentException($"Unknown command '{command.Name}'.");

    #endregion
}