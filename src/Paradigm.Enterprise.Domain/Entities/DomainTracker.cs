namespace Paradigm.Enterprise.Domain.Entities
{
    public class DomainTracker<TEntity> where TEntity : Interfaces.IEntity
    {
        #region Properties

        /// <summary>
        /// Gets the added entities.
        /// </summary>
        /// <value>
        /// The added.
        /// </value>
        public IReadOnlyCollection<TEntity> Added => _addedList;

        /// <summary>
        /// Gets the edited entities.
        /// </summary>
        /// <value>
        /// The edited.
        /// </value>
        public IReadOnlyCollection<TEntity> Edited => _editedList;

        /// <summary>
        /// Gets the removed entities.
        /// </summary>
        /// <value>
        /// The removed.
        /// </value>
        public IReadOnlyCollection<TEntity> Removed => _removedList;

        /// <summary>
        /// The added list
        /// </summary>
        private readonly List<TEntity> _addedList;

        /// <summary>
        /// The edited list
        /// </summary>
        private readonly List<TEntity> _editedList;

        /// <summary>
        /// The removed list
        /// </summary>
        private readonly List<TEntity> _removedList;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainTracker{TEntity}"/> class.
        /// </summary>
        public DomainTracker()
        {
            _removedList = [];
            _editedList = [];
            _addedList = [];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers the specified entity as added.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void Add(TEntity entity) => _addedList.Add(entity);

        /// <summary>
        /// Registers the specified entity as edited.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void Edit(TEntity entity) => _editedList.Add(entity);

        /// <summary>
        /// Registers the specified entity as removed.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void Remove(TEntity entity) => _removedList.Add(entity);

        #endregion
    }
}