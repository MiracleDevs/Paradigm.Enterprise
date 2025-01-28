using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Paradigm.Enterprise.Providers.Utils;

public sealed class AsyncProcessManager
{
    #region Properties

    /// <summary>
    /// The service scope factory
    /// </summary>
    private readonly IServiceScopeFactory _serviceScopeFactory;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncProcessManager" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    public AsyncProcessManager(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes the specified task.
    /// </summary>
    /// <param name="task">The task.</param>
    public void Execute(Func<IServiceProvider, Task> task)
    {
        Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();

            try
            {
                await task(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                scope.ServiceProvider.GetRequiredService<ILogger<AsyncProcessManager>>().LogError(ex, "Failed to execute async process.");
            }
        });
    }

    #endregion
}