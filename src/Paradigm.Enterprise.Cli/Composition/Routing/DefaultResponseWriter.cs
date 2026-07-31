namespace Paradigm.Enterprise.Cli;
internal sealed class DefaultResponseWriter : IResponseWriter
{
#region Public Methods
    public Task WriteAsync(CommandResponse response, OutputFormat format, TextWriter output, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ResponseWriter.WriteAsync(response, format, output);
    }
#endregion
}
