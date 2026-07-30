namespace Paradigm.Enterprise.Domain.Entities;

/// <summary>
/// Records domain entities that an aggregate has staged for addition, modification, or removal.
/// </summary>
/// <typeparam name="TEntity">The entity type tracked by the aggregate.</typeparam>
/// <remarks>
/// Tracking is in memory only. Calling <see cref="Add"/>, <see cref="Edit"/>, or <see cref="Remove"/>
/// does not persist data and does not move an entity out of another collection. Duplicate registrations
/// are retained. A repository or aggregate mapper is responsible for consuming the collections.
/// </remarks>
/// <example>
/// <code>
/// var tracker = new DomainTracker&lt;OrderLine&gt;();
/// tracker.Add(newLine);
/// tracker.Remove(discontinuedLine);
/// tracker.Reset();
/// </code>
/// </example>
public class DomainTracker<TEntity> where TEntity : Interfaces.IEntity
{
    #region Properties

    /// <summary>
    /// Gets the entities staged for addition.
    /// </summary>
    /// <value>
    /// A read-only view of the added collection, in registration order. Duplicate entries are preserved.
    /// </value>
    public IReadOnlyCollection<TEntity> Added => _addedList;

    /// <summary>
    /// Gets the entities staged for modification.
    /// </summary>
    /// <value>
    /// A read-only view of the edited collection, in registration order. Duplicate entries are preserved.
    /// </value>
    public IReadOnlyCollection<TEntity> Edited => _editedList;

    /// <summary>
    /// Gets the entities staged for removal.
    /// </summary>
    /// <value>
    /// A read-only view of the removed collection, in registration order. Duplicate entries are preserved.
    /// </value>
    public IReadOnlyCollection<TEntity> Removed => _removedList;

    /// <summary>
    /// Stores entities in the order they were staged for addition.
    /// </summary>
    private readonly List<TEntity> _addedList;

    /// <summary>
    /// Stores entities in the order they were staged for modification.
    /// </summary>
    private readonly List<TEntity> _editedList;

    /// <summary>
    /// Stores entities in the order they were staged for removal.
    /// </summary>
    private readonly List<TEntity> _removedList;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainTracker{TEntity}"/> class.
    /// </summary>
    public DomainTracker()
    {
        _removedList = new List<TEntity>();
        _editedList = new List<TEntity>();
        _addedList = new List<TEntity>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines whether the <see cref="Added"/>, <see cref="Edited"/>, and <see cref="Removed"/> collections are empty.
    /// </summary>
    /// <returns><c>true</c> if all collections are empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty() => !_addedList.Any() && !_editedList.Any() && !_removedList.Any();

    /// <summary>
    /// Stages an entity for addition.
    /// </summary>
    /// <param name="entity">The entity to append to the added collection.</param>
    public void Add(TEntity entity) => _addedList.Add(entity);

    /// <summary>
    /// Stages an entity for modification.
    /// </summary>
    /// <param name="entity">The entity to append to the edited collection.</param>
    public void Edit(TEntity entity) => _editedList.Add(entity);

    /// <summary>
    /// Stages an entity for removal.
    /// </summary>
    /// <param name="entity">The entity to append to the removed collection.</param>
    public void Remove(TEntity entity) => _removedList.Add(entity);

    /// <summary>
    /// Clears the <see cref="Added"/>, <see cref="Edited"/>, and <see cref="Removed"/> collections.
    /// </summary>
    public void Reset()
    {
        _addedList.Clear();
        _editedList.Clear();
        _removedList.Clear();
    }

    #endregion
}
