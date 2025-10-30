using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Xml.XPath;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xml;

internal class XmlTableSchema : TableSchemaBase
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
    /// Initializes a new instance of the <see cref="XmlTableSchema"/> class.
    /// </summary>
    /// <param name="xmlNavigator">The XML navigator.</param>
    public XmlTableSchema(XPathNavigator xmlNavigator)
    {
        XmlNavigator = xmlNavigator;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Gets the table schema columns.
    /// </summary>
    /// <returns></returns>
    protected override IList<IColumn> GetTableSchemaColumns()
    {
        var columnsList = new List<IColumn>();

        XmlNavigator.MoveToRoot();
        XmlNavigator.MoveToFirstChild(); //Move to skulist node
        XmlNavigator.MoveToFirstChild(); //Move to first item node

        if (XmlNavigator.MoveToFirstChild()) //Move to item first property
        {
            var properties = new List<string>();

            do
                properties.Add(XmlNavigator.Name);
            while (XmlNavigator.MoveToNext());

            GenerateColumnsNames(properties.Count);

            columnsList = properties
                .Select((x, i) =>
                {
                    var columnName = SourceHasHeader ? x : GetGeneratedColumnName(i);
                    return new XmlColumn(i, columnName, typeof(string));
                })
                .Cast<IColumn>()
                .ToList();
        }

        XmlNavigator.MoveToRoot(); //Reset the navigator back to root

        return columnsList;
    }

    #endregion
}