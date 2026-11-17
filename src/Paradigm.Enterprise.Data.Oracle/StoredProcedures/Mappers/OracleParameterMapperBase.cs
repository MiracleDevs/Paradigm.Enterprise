using Oracle.ManagedDataAccess.Client;

namespace Paradigm.Enterprise.Data.Oracle.StoredProcedures.Mappers;

/// <summary>Builds fresh Oracle parameters from an application parameter object.</summary>
/// <remarks>Use explicit provider metadata for values whose database type cannot be inferred, especially nulls.</remarks>
public abstract class OracleParameterMapperBase : IOracleParameterMapper
{
    #region Fields

    private readonly List<OracleParameter> _parameters = [];

    #endregion

    #region Public Methods

    /// <summary>Maps one object without accumulating parameters from earlier calls.</summary>
    /// <param name="parameters">The application parameter object.</param>
    /// <returns>A new array of independent Oracle parameters.</returns>
    public OracleParameter[] Map(object parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        _parameters.Clear();
        AddOracleParameters(parameters);
        return [.. _parameters];
    }

    /// <summary>Releases the mapper's parameter references without invalidating returned parameters.</summary>
    public void Dispose() => _parameters.Clear();

    #endregion

    #region Protected Methods

    /// <summary>Adds parameters represented by the application's object.</summary>
    /// <param name="parameters">The application parameter object.</param>
    protected abstract void AddOracleParameters(object parameters);

    /// <summary>Adds a parameter, translating a null value to <see cref="DBNull.Value"/>.</summary>
    /// <param name="name">The provider parameter name, conventionally <c>Name</c>.</param>
    /// <param name="value">The bound value.</param>
    protected void AddOracleParameter(string name, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _parameters.Add(new OracleParameter { ParameterName = name, Value = value ?? DBNull.Value });
    }

    /// <summary>Adds an independent copy of a fully specified parameter and its provider metadata.</summary>
    /// <param name="parameter">The parameter with type, size, precision, scale, direction, and value.</param>
    /// <remarks>The supplied parameter is not attached to the command or mutated by the mapper.</remarks>
    protected void AddOracleParameter(OracleParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        var copy = (OracleParameter)((ICloneable)parameter).Clone();
        copy.Value ??= DBNull.Value;
        _parameters.Add(copy);
    }

    #endregion
}
