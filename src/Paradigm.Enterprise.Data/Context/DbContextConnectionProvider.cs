using Microsoft.Extensions.Configuration;

namespace Paradigm.Enterprise.Data.Context
{
    public class DbContextConnectionProvider : IDisposable
    {
        #region Properties

        protected readonly IConfiguration _configuration;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextConnectionProvider"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public DbContextConnectionProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}