namespace Paradigm.Enterprise.Tests.UnitOfWork;

[TestClass]
[ExcludeFromCodeCoverage]
public class UnitOfWorkTests
{
    private Mock<ICommiteable>? _mockCommiteable1;
    private Mock<ICommiteable>? _mockCommiteable2;
    private Mock<ITransaction>? _mockTransaction;
    private Data.Uow.UnitOfWork? _unitOfWork;

    [TestInitialize]
    public void Initialize()
    {
        _mockCommiteable1 = new Mock<ICommiteable>();
        _mockCommiteable2 = new Mock<ICommiteable>();
        _mockTransaction = new Mock<ITransaction>();
        _unitOfWork = new Data.Uow.UnitOfWork();

        _mockCommiteable1.Setup(m => m.CreateTransaction()).Returns(_mockTransaction.Object);
        _mockTransaction.Setup(m => m.IsActive).Returns(true);
    }

    [TestMethod]
    public void RegisterCommiteable_ShouldAddCommiteable()
    {
        // Arrange

        // Act
        _unitOfWork!.RegisterCommiteable(_mockCommiteable1!.Object);

        // Assert
        Assert.IsFalse(_unitOfWork.HasActiveTransaction);
    }

    [TestMethod]
    public void CreateTransaction_ShouldCreateTransactionFromFirstCommiteable()
    {
        // Arrange
        _unitOfWork!.RegisterCommiteable(_mockCommiteable1!.Object);

        // Act
        var transaction = _unitOfWork.CreateTransaction();

        // Assert
        Assert.IsNotNull(transaction);
        Assert.IsTrue(_unitOfWork.HasActiveTransaction);
        _mockCommiteable1.Verify(m => m.CreateTransaction(), Times.Once);
    }

    [TestMethod]
    public void CreateTransaction_ShouldAddOtherCommiteables()
    {
        // Arrange
        _unitOfWork!.RegisterCommiteable(_mockCommiteable1!.Object);
        _unitOfWork.RegisterCommiteable(_mockCommiteable2!.Object);

        // Act
        var transaction = _unitOfWork.CreateTransaction();

        // Assert
        Assert.IsNotNull(transaction);
        _mockTransaction!.Verify(m => m.AddCommiteable(_mockCommiteable2.Object), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void CreateTransaction_ShouldThrowIfNoCommiteables()
    {
        // Act
        _unitOfWork!.CreateTransaction();
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void CreateTransaction_ShouldThrowIfActiveTransaction()
    {
        // Arrange
        _unitOfWork!.RegisterCommiteable(_mockCommiteable1!.Object);
        _unitOfWork.CreateTransaction();

        // Act
        _unitOfWork.CreateTransaction();
    }

    [TestMethod]
    public async Task CommitChangesAsync_ShouldCallCommitOnAllCommiteables()
    {
        // Arrange
        _unitOfWork!.RegisterCommiteable(_mockCommiteable1!.Object);
        _unitOfWork.RegisterCommiteable(_mockCommiteable2!.Object);

        // Act
        await _unitOfWork.CommitChangesAsync();

        // Assert
        _mockCommiteable1.Verify(m => m.CommitChangesAsync(), Times.Once);
        _mockCommiteable2.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public void UseCurrentTransaction_ShouldReturnCurrentTransaction()
    {
        // Arrange
        _unitOfWork!.RegisterCommiteable(_mockCommiteable1!.Object);
        _unitOfWork.CreateTransaction();

        // Act
        var transaction = _unitOfWork.UseCurrentTransaction();

        // Assert
        Assert.IsNotNull(transaction);
        Assert.AreEqual(_mockTransaction!.Object, transaction);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void UseCurrentTransaction_ShouldThrowIfNoActiveTransaction()
    {
        // Act
        _unitOfWork!.UseCurrentTransaction();
    }
}