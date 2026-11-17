using Moq;
using MySql.Data.MySqlClient;
using Paradigm.Enterprise.Data.MySql.StoredProcedures;
using Paradigm.Enterprise.Data.MySql.StoredProcedures.Mappers;
using System.Data;
using System.Data.Common;

namespace Paradigm.Enterprise.Data.MySql.Tests;

[TestClass]
public sealed class ProcedureContractTests
{
    #region Nested Types

    private sealed class NoResultProcedure : StoredProcedureBase
    {
        #region Overrides

        protected override string StoredProcedureName => "NeverExecuted";

        #endregion
    }

    private sealed record FailingParameters;

    private sealed class FailingMapper : IMySqlParameterMapper
    {
        #region Public Methods

        public MySqlParameter[] Map(object parameters) => throw new FormatException("original mapping failure");

        public void Dispose() => throw new InvalidOperationException("secondary disposal failure");

        #endregion
    }

    private sealed class FailingMappingProcedure : StoredProcedureBase<FailingParameters>
    {
        #region Overrides

        protected override string StoredProcedureName => "NeverExecuted";

        #endregion
    }

    #endregion

    #region Public Methods

    [TestMethod]
    public async Task Non_mysql_connection_is_rejected_before_opening_or_creating_command()
    {
        var connection = new Mock<DbConnection>(MockBehavior.Strict);
        connection.SetupGet(value => value.State).Returns(ConnectionState.Closed);

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new NoResultProcedure().ExecuteAsync(connection.Object));

        connection.Verify(value => value.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
        connection.Verify(value => value.Open(), Times.Never);
    }

    [TestMethod]
    public async Task Parameter_mapping_exception_survives_mapper_disposal_failure_before_connection_open()
    {
        MySqlParameterMapperFactory.RegisterMapper<FailingParameters>(() => new FailingMapper());
        await using var connection = new MySqlConnection("Server=127.0.0.1;Port=1;Database=synthetic;Connection Timeout=1");

        var exception = await Assert.ThrowsExactlyAsync<FormatException>(() => new FailingMappingProcedure()
            .ExecuteAsync(connection, new FailingParameters()));

        Assert.AreEqual("original mapping failure", exception.Message);
        Assert.AreEqual(ConnectionState.Closed, connection.State);
    }

    [TestMethod]
    public async Task Parameter_mapping_failure_leaves_an_already_open_caller_connection_untouched()
    {
        MySqlParameterMapperFactory.RegisterMapper<FailingParameters>(() => new FailingMapper());
        var connection = new Mock<DbConnection>(MockBehavior.Strict);
        connection.SetupGet(value => value.State).Returns(ConnectionState.Open);

        var exception = await Assert.ThrowsExactlyAsync<FormatException>(() => new FailingMappingProcedure()
            .ExecuteAsync(connection.Object, new FailingParameters()));

        Assert.AreEqual("original mapping failure", exception.Message);
        Assert.AreEqual(ConnectionState.Open, connection.Object.State);
        connection.Verify(value => value.Close(), Times.Never);
        connection.Verify(value => value.CloseAsync(), Times.Never);
        connection.Verify(value => value.Open(), Times.Never);
        connection.Verify(value => value.OpenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}