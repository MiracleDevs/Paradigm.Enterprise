namespace Paradigm.Enterprise.Cli;

internal interface IResponseWriter
{
    Task WriteAsync(CommandResponse response, OutputFormat format, TextWriter output, CancellationToken cancellationToken);
}