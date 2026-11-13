using MySql.Data.MySqlClient;
using Paradigm.Enterprise.Data.MySql.StoredProcedures;
using Paradigm.Enterprise.Data.MySql.StoredProcedures.Mappers;
using System.Data;

namespace Paradigm.Enterprise.Data.MySql.Tests;

[TestClass]
public sealed class ParameterMappingTests
{
    #region Nested Types

    private sealed record InvoiceParameters(decimal Amount, string? Note);

    private sealed record FactoryParameters;

    private sealed record MissingParameters;

    private sealed class InvoiceMapper : MySqlParameterMapperBase
    {
        #region Fields

        private readonly MySqlParameter _amount = new("@Amount", MySqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 4,
            Direction = ParameterDirection.Input
        };

        #endregion

        #region Overrides

        protected override void AddMySqlParameters(object parameters)
        {
            var invoice = (InvoiceParameters)parameters;
            _amount.Value = invoice.Amount;
            AddMySqlParameter(_amount);
            AddMySqlParameter("@Note", invoice.Note);
        }

        #endregion
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public void Parameters_preserve_metadata_and_nulls_after_mapper_disposal()
    {
        MySqlParameter[] parameters;
        using (var mapper = new InvoiceMapper())
            parameters = mapper.Map(new InvoiceParameters(123.4567m, null));

        Assert.HasCount(2, parameters);
        Assert.AreEqual("@Amount", parameters[0].ParameterName);
        Assert.AreEqual(MySqlDbType.Decimal, parameters[0].MySqlDbType);
        Assert.AreEqual((byte)18, parameters[0].Precision);
        Assert.AreEqual((byte)4, parameters[0].Scale);
        Assert.AreEqual(ParameterDirection.Input, parameters[0].Direction);
        Assert.AreEqual(123.4567m, parameters[0].Value);
        Assert.AreEqual(DBNull.Value, parameters[1].Value);

        using var command = new MySqlCommand();
        command.Parameters.AddRange(parameters);
        Assert.HasCount(2, command.Parameters);
    }

    [TestMethod]
    public void Repeated_mapping_creates_independent_parameters_without_accumulating_values()
    {
        using var mapper = new InvoiceMapper();
        var first = mapper.Map(new InvoiceParameters(1m, "first"));
        var second = mapper.Map(new InvoiceParameters(2m, "second"));

        Assert.HasCount(2, first);
        Assert.HasCount(2, second);
        Assert.AreNotSame(first[0], second[0]);
        Assert.AreEqual(1m, first[0].Value);
        Assert.AreEqual(2m, second[0].Value);
        Assert.AreEqual("first", first[1].Value);
        Assert.AreEqual("second", second[1].Value);
    }

    [TestMethod]
    public void Mapper_factory_is_first_registration_wins_and_creates_fresh_instances()
    {
        MySqlParameterMapperFactory.RegisterMapper<FactoryParameters>(() => new InvoiceMapper());
        MySqlParameterMapperFactory.AddCustomMapper<FactoryParameters>(() => throw new InvalidOperationException("second registration"));
        using var first = MySqlParameterMapperFactory.GetMapper<FactoryParameters>();
        using var second = MySqlParameterMapperFactory.GetMapper(typeof(FactoryParameters));

        Assert.IsInstanceOfType<InvoiceMapper>(first);
        Assert.AreNotSame(first, second);
    }

    [TestMethod]
    public void Missing_mapper_names_the_application_parameter_type()
    {
        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => MySqlParameterMapperFactory.GetMapper<MissingParameters>());

        StringAssert.Contains(exception.Message, nameof(MissingParameters));
    }

    #endregion
}