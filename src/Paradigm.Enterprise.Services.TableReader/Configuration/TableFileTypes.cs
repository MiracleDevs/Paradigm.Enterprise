namespace Paradigm.Enterprise.Services.TableReader.Configuration;

/// <summary>
/// Identifies the supported tabular document formats.
/// </summary>
public enum TableFileTypes
{
    /// <summary>Comma-separated or otherwise delimiter-separated text.</summary>
    Csv = 0,
    /// <summary>Microsoft Excel binary or Open XML workbook data.</summary>
    Xls = 1,
    /// <summary>XML tabular data.</summary>
    Xml = 2,
    /// <summary>JSON tabular data.</summary>
    Json = 3
}
