using Aspire.Hosting.ApplicationModel;

namespace Paradigm.Enterprise.Aspire.Hosting;

/// <summary>
/// Represents a reusable infrastructure reference that can be attached to a project resource.
/// </summary>
public class ParadigmResourceReference
{
    #region Fields

    private readonly Action<IResourceBuilder<ProjectResource>> _configureConsumer;

    #endregion

    #region Constructors

    internal ParadigmResourceReference(Action<IResourceBuilder<ProjectResource>> configureConsumer)
    {
        _configureConsumer = configureConsumer;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Attaches this dependency and its startup ordering to a project resource.
    /// </summary>
    /// <param name="project">The consuming project resource.</param>
    public void ConfigureConsumer(IResourceBuilder<ProjectResource> project)
    {
        ArgumentNullException.ThrowIfNull(project);

        _configureConsumer(project);
    }

    #endregion
}
