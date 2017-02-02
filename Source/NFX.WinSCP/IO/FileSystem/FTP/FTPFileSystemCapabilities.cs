namespace NFX.IO.FileSystem.FTP
{
  public class FTPFileSystemCapabilities : IFileSystemCapabilities
  {
    #region CONSTS
    internal static readonly char[] PATH_SEPARATORS = new char[] { '/' };
    #endregion

    #region Static
    private static FTPFileSystemCapabilities s_Instance = new FTPFileSystemCapabilities();

    public static FTPFileSystemCapabilities Instance { get { return s_Instance; } }
    #endregion

    #region .ctor
    public FTPFileSystemCapabilities() { }
    #endregion

    #region Public
    public bool SupportsVersioning { get { return false; } }

    public bool SupportsTransactions { get { return false; } }

    public int MaxFilePathLength { get { return 255; } }

    public int MaxFileNameLength { get { return 255; } }

    public int MaxDirectoryNameLength { get { return 255; } }

    public ulong MaxFileSize { get { return 2 * (2 ^ 30); } }

    public char[] PathSeparatorCharacters { get { return PATH_SEPARATORS; } }

    public bool IsReadonly { get { return false; } }

    public bool SupportsSecurity { get { return false; } }

    public bool SupportsCustomMetadata { get { return false; } }

    public bool SupportsDirectoryRenaming { get { return true; } }

    public bool SupportsFileRenaming { get { return true; } }

    public bool SupportsStreamSeek { get { return true; } }

    public bool SupportsFileModification { get { return true; } }

    public bool SupportsCreationTimestamps { get { return false; } }

    public bool SupportsModificationTimestamps { get { return true; } }

    public bool SupportsLastAccessTimestamps { get { return false; } }

    public bool SupportsReadonlyDirectories { get { return false; } }

    public bool SupportsReadonlyFiles { get { return false; } }

    public bool SupportsCreationUserNames { get { return false; } }

    public bool SupportsModificationUserNames { get { return false; } }

    public bool SupportsLastAccessUserNames { get { return false; } }

    public bool SupportsFileSizes { get { return true; } }

    public bool SupportsDirectorySizes { get { return false; } }

    public bool SupportsAsyncronousAPI { get { return false; } }
    #endregion
  }
}
