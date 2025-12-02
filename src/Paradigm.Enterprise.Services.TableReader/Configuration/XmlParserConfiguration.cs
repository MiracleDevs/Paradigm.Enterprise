using System.Text;

namespace Paradigm.Enterprise.Services.TableReader.Configuration;

public class XmlParserConfiguration
{
    public bool? Indent { get; set; }
    public string? IndentChars { get; set; }
    public Encoding? Enconding { get; set; }
    public bool OmitXmlDeclaration { get; set; }
}
