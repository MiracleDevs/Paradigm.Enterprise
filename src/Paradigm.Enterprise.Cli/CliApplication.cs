namespace Paradigm.Enterprise.Cli;
internal sealed class CliApplication(CommandRouter router, IResponseWriter responseWriter, IExitPolicy exitPolicy, TextWriter output, TextWriter error)
{
#region Public Methods
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!CommandLine.TryParse(args, out var command, out var parseError))
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync(CommandLine.Usage);
            return 2;
        }

        try
        {
            var response = await router.RouteAsync(command!, cancellationToken);
            await responseWriter.WriteAsync(response, command!.Format, output, cancellationToken);
            return exitPolicy.GetExitCode(response);
        }
        catch (ArgumentException exception)
        {
            if (command!.Format == OutputFormat.Json)
                await responseWriter.WriteAsync(ResponseFactory.Create(command!.Name, [], [], [new("PE0002", "error", exception.Message)]), command.Format, output, cancellationToken);
            else
                await error.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (OperationCanceledException)when (cancellationToken.IsCancellationRequested)
        {
            var response = ResponseFactory.Create(command!.Name, [], [], [new("PE5002", "error", "The command was canceled.")]);
            await responseWriter.WriteAsync(response, command.Format, output, CancellationToken.None);
            return 1;
        }
        catch (Exception exception)when (exception is AssetsException or DirectoryNotFoundException or FileNotFoundException or BadImageFormatException)
        {
            var response = ResponseFactory.Create(command!.Name, [], [], [new("PE1002", "error", exception.Message)]);
            await responseWriter.WriteAsync(response, command.Format, output, cancellationToken);
            return 3;
        }
        catch (Exception exception)
        {
            var response = ResponseFactory.Create(command!.Name, [], [], [new("PE1002", "error", $"Metadata resolution failed: {exception.Message}")]);
            await responseWriter.WriteAsync(response, command.Format, output, cancellationToken);
            return 3;
        }
    }
#endregion
}
