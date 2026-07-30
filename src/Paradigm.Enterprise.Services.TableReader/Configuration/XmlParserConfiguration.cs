using System.Text;

namespace Paradigm.Enterprise.Services.TableReader.Configuration;

/// <summary>
/// Configures XML output formatting and encoding.
/// </summary>
public class XmlParserConfiguration
{
    /// <summary>Gets or sets whether nested XML elements are indented.</summary>
    public bool? Indent { get; set; }
    /// <summary>Gets or sets the characters used for each indentation level.</summary>
    public string? IndentChars { get; set; }
    /// <summary>Gets or sets the encoding written in the XML declaration.</summary>
    /// <remarks>The property name is retained for binary compatibility.</remarks>
    public Encoding? Enconding { get; set; }
    /// <summary>Gets or sets whether the XML declaration is omitted.</summary>
    public bool OmitXmlDeclaration { get; set; }
}
