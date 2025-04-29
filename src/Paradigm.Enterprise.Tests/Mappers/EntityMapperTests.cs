using Paradigm.Enterprise.Domain.Mappers;

namespace Paradigm.Enterprise.Tests.Mappers;

[TestClass]
[ExcludeFromCodeCoverage]
public class EntityMapperTests
{
    // Sample entity for testing
    public class TestEntity : EntityBase, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public new bool IsNew() => Id == 0;
    }

    // Sample entity for mapping to/from
    public class TestEntityDto : EntityBase, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public new bool IsNew() => Id == 0;
    }

    // Test mapper implementation
    public class TestEntityMapper : MapperBase<TestEntity, TestEntityDto>
    {
        public override TestEntityDto MapTo(TestEntity source, TestEntityDto destination)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            return destination;
        }

        public override TestEntity MapFrom(TestEntity destination, TestEntityDto source)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            return destination;
        }
    }

    private TestEntityMapper? _mapper;

    [TestInitialize]
    public void Initialize()
    {
        _mapper = new TestEntityMapper();
    }

    [TestMethod]
    public void Map_Entity_To_Dto_Should_Transfer_Properties()
    {
        // Arrange
        var entity = new TestEntity
        {
            Id = 1,
            Name = "Test Entity"
        };

        // Act
        var dto = _mapper!.MapTo(entity, new TestEntityDto());

        // Assert
        Assert.IsNotNull(dto);
        Assert.AreEqual(1, dto.Id);
        Assert.AreEqual("Test Entity", dto.Name);
    }

    [TestMethod]
    public void Map_Dto_To_Entity_Should_Transfer_Properties()
    {
        // Arrange
        var dto = new TestEntityDto
        {
            Id = 2,
            Name = "Test DTO"
        };

        // Act
        var entity = _mapper!.MapFrom(new TestEntity(), dto);

        // Assert
        Assert.IsNotNull(entity);
        Assert.AreEqual(2, entity.Id);
        Assert.AreEqual("Test DTO", entity.Name);
    }
} 