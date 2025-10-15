using Paradigm.Enterprise.Services.TableReader.Readers.Base;
using System.Xml.XPath;

namespace Paradigm.Enterprise.Services.TableReader.Readers.Xml;

internal class XmlRow : RowBase
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
    /// Initializes a new instance of the <see cref="XmlRow"/> class.
    /// </summary>
    /// <param name="tableSchema">The table schema.</param>
    public XmlRow(ITableSchema tableSchema, XPathNavigator xmlNavigator) : base(tableSchema)
    {
        XmlNavigator = xmlNavigator;
    }

    #endregion

    #region Abstract implementations

    /// <summary>
    /// Reads a new row.
    /// </summary>
    /// <returns></returns>
    public override bool Read()
    {
        if (string.IsNullOrEmpty(XmlNavigator.Name))
        {
            XmlNavigator.MoveToFirstChild(); //Move to skulist node
            XmlNavigator.MoveToFirstChild(); //Move to first item node
        }
        else if (!XmlNavigator.MoveToNext())
            return false;

        FillValues();
        Index++;

        if (Values.Count == 0 || Values.Count == 1 && string.IsNullOrEmpty(Values[0]))
            return false;

        ValidateValuesSchema();

        return true;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Fills the values from the source file.
    /// </summary>
    private void FillValues()
    {
        Values = [];

        XmlNavigator.MoveToFirstChild(); //Move to item first property

        do
            Values.Add(XmlNavigator.Value);
while (XmlNavigator.MoveToNext());

        XmlNavigator.MoveToParent();
    }

    #endregion
}