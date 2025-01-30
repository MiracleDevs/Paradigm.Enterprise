namespace Paradigm.Enterprise.Interfaces;
public interface IEntity
{
    int Id { get; }

    bool IsNew();
}