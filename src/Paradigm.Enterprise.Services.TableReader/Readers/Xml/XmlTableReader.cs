using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Xml.XPath;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xml;

internal class XmlTableReader : TableReaderBase
{
    #region Properties

    /// <summary>
    /// Gets the XML navigator.
    /// </summary>
    /// <value>
    /// The XML navigator.
    /// </value>
    private XPathNavigator XmlNavigator { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlTableReader" /> class.
    /// </summary>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    private XmlTableReader(Stream contentStream, bool sourceHasHeader)
    {
        XmlNavigator = new XPathDocument(contentStream).CreateNavigator();
        Schema = new XmlTableSchema(XmlNavigator).Initialize(sourceHasHeader);
        CurrentRow = new XmlRow(Schema, XmlNavigator);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Opens a new table reader from the specified content.
    /// </summary>
    /// <param name="content">The content.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static ITableReader OpenFromContent(byte[] content, bool sourceHasHeader)
    {
        return new XmlTableReader(new MemoryStream(content), sourceHasHeader);
    }

    /// <summary>
    /// Opens a new table reader from the specified stream.
    /// </summary>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="sourceHasHeader">if set to <c>true</c> [source has header].</param>
    /// <returns></returns>
    public static ITableReader OpenFromStream(Stream contentStream, bool sourceHasHeader)
    {
        return new XmlTableReader(contentStream, sourceHasHeader);
    }

    #endregion
}