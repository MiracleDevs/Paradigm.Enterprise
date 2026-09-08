namespace BeaconAr.CodeGenerator;

internal sealed class AtomicOutputDirectory : IDisposable
{
    #region Fields

    private readonly string _targetPath;
    private readonly string _stagingPath;
    private bool _committed;

    #endregion

    #region Properties

    public string Path => _stagingPath;

    #endregion

    #region Constructors

    public AtomicOutputDirectory(string targetPath)
    {
        _targetPath = System.IO.Path.GetFullPath(targetPath);
        var parent = Directory.GetParent(_targetPath)
            ?? throw new InvalidOperationException("A generated output directory cannot be a file-system root.");
        Directory.CreateDirectory(parent.FullName);
        _stagingPath = System.IO.Path.Combine(parent.FullName, $".{System.IO.Path.GetFileName(_targetPath)}.staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_stagingPath);
    }

    #endregion

    #region Public Methods

    public void Commit()
    {
        var backupPath = $"{_targetPath}.backup-{Guid.NewGuid():N}";
        var hadTarget = Directory.Exists(_targetPath);
        if (hadTarget)
            Directory.Move(_targetPath, backupPath);

        try
        {
            Directory.Move(_stagingPath, _targetPath);
            _committed = true;
            if (hadTarget)
                Directory.Delete(backupPath, recursive: true);
        }
        catch
        {
            if (!Directory.Exists(_targetPath) && hadTarget && Directory.Exists(backupPath))
                Directory.Move(backupPath, _targetPath);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_committed && Directory.Exists(_stagingPath))
            Directory.Delete(_stagingPath, recursive: true);
    }

    #endregion
}
