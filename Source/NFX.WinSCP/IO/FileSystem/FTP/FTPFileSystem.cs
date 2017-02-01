using System;
using System.Collections.Generic;
using System.IO;

using NFX;
using NFX.Environment;

using WinSCP;

namespace NFX.IO.FileSystem.SFTP
{
  public class FTPFileSystem : FileSystem
  {
    internal class Handle : IFileSystemHandle
    {
      public Handle(RemoteFileInfo fileInfo) { FileInfo = fileInfo; }
      public readonly RemoteFileInfo FileInfo;
    }

    public FTPFileSystem(string name, IConfigSectionNode node = null) : base(name, node)
    {
    }

    public override string ComponentCommonName { get { return "fsftp"; } }

    public override IFileSystemCapabilities GeneralCapabilities {  get { throw new NotImplementedException(); } }
    public override IFileSystemCapabilities InstanceCapabilities { get { throw new NotImplementedException(); } }

    protected override FileSystemSessionConnectParams MakeSessionConfigParams(IConfigSectionNode node)
    {
      return FileSystemSessionConnectParams.Make<FTPFileSystemSessionConnectParams>(node);
    }

    public FTPFileSystemSession StartSession(FTPFileSystemSessionConnectParams cParams)
    {
      var sftpCParams = cParams ?? (DefaultSessionConnectParams as FTPFileSystemSessionConnectParams);
      if (sftpCParams == null)
        throw new NFXException(NFX.Web.StringConsts.FS_SESSION_BAD_PARAMS_ERROR + this.GetType() + ".StartSession");

      return new FTPFileSystemSession(this, null, sftpCParams);
    }

    public override FileSystemSession StartSession(FileSystemSessionConnectParams cParams = null)
    {
      return this.StartSession(cParams as FTPFileSystemSessionConnectParams);
    }

    protected override FileSystemDirectory DoCreateDirectory(FileSystemDirectory directory, string name)
    {
      var session = directory.Session as FTPFileSystemSession;
      var handle = directory.Handle as Handle;
      var path = session.Connection.CombinePaths(handle.FileInfo.FullName, name);
      session.Connection.CreateDirectory(path);

      var dirInfo = session.Connection.GetFileInfo(path);
      return new FileSystemDirectory(directory.Session, directory.Path, name, new Handle(dirInfo));
    }

    protected override FileSystemFile DoCreateFile(FileSystemDirectory directory, string name, int size)
    {
      var localFile = Path.GetTempFileName();
      using (var fs = new FileStream(localFile, FileMode.Open, FileAccess.Write, FileShare.None))
        fs.SetLength(size);
      return doCreateFile(directory, name, localFile, remove: true);
    }

    protected override FileSystemFile DoCreateFile(FileSystemDirectory directory, string name, string localFile, bool readOnly)
    {
      return doCreateFile(directory, name, localFile);
    }

    private FileSystemFile doCreateFile(FileSystemDirectory directory, string name, string localFile, bool remove = false)
    {
      var session = directory.Session as FTPFileSystemSession;
      var handle = directory.Handle as Handle;
      var path = session.Connection.CombinePaths(handle.FileInfo.FullName, name);
      var options = new TransferOptions { TransferMode = TransferMode.Binary };
      var result = session.Connection.PutFiles(localFile, path, remove: remove, options: options);
      result.Check();
      var fileInfo = session.Connection.GetFileInfo(path);
      return new FileSystemFile(directory.Session, directory.Path, name, new Handle(fileInfo));
    }

    protected override void DoDeleteItem(FileSystemSessionItem item)
    {
      var session = item.Session as FTPFileSystemSession;
      var handle = item.Handle as Handle;
      var result = session.Connection.RemoveFiles(handle.FileInfo.FullName);
      result.Check();
    }

    protected override IEnumerable<string> DoGetFileNames(FileSystemDirectory directory, bool recursive)
    {
      var session = directory.Session as FTPFileSystemSession;
      var handle = directory.Handle as Handle;

      var result = session.Connection.EnumerateRemoteFiles(handle.FileInfo.FullName, null, recursive ? EnumerationOptions.AllDirectories : EnumerationOptions.None );
      foreach (RemoteFileInfo item in result)
        yield return item.Name;
    }

    protected override IEnumerable<string> DoGetSubDirectoryNames(FileSystemDirectory directory, bool recursive)
    {
      var session = directory.Session as FTPFileSystemSession;
      var handle = directory.Handle as Handle;

      var result = session.Connection.EnumerateRemoteFiles(handle.FileInfo.FullName, null, (recursive ? EnumerationOptions.AllDirectories : EnumerationOptions.None) | EnumerationOptions.EnumerateDirectories);
      foreach (RemoteFileInfo item in result)
        if (item.IsDirectory) yield return item.Name;
    }

    protected override bool DoRenameItem(FileSystemSessionItem item, string newName)
    {
      var session = item.Session as FTPFileSystemSession;
      var handle = item.Handle as Handle;

      var path = session.Connection.CombinePaths(handle.FileInfo.FullName.Substring(0, handle.FileInfo.Name.Length), newName);
      session.Connection.MoveFile(handle.FileInfo.FullName, path);

      var fileInfo = session.Connection.GetFileInfo(path);
      return true;
    }

    protected override FileSystemSessionItem DoNavigate(FileSystemSession ses, string path)
    {
      var session = ses as FTPFileSystemSession;
      var fileInfo = session.Connection.GetFileInfo(path);
      if (fileInfo.IsDirectory)
        return new FileSystemDirectory(session,
          fileInfo.FullName.Length == fileInfo.Name.Length ? null : fileInfo.FullName.Substring(0, fileInfo.Name.Length),
          fileInfo.Name, new Handle(fileInfo));
      return new FileSystemFile(ses,
          fileInfo.FullName.Length == fileInfo.Name.Length ? null : fileInfo.FullName.Substring(0, fileInfo.Name.Length),
          fileInfo.Name, new Handle(fileInfo));
    }

    protected override FileSystemStream DoGetFileStream(FileSystemFile file, Action<FileSystemStream> disposeAction)
    {
      return new FTPFileSystemStream(file, disposeAction);
    }

    protected override ulong DoGetItemSize(FileSystemSessionItem item) { return (ulong)(item.Handle as Handle).FileInfo.Length; }

    protected override DateTime? DoGetModificationTimestamp(FileSystemSessionItem item) { return (item.Handle as Handle).FileInfo.LastWriteTime; }

    protected override DateTime? DoGetLastAccessTimestamp(FileSystemSessionItem item) { throw new NotImplementedException(); }

    protected override DateTime? DoGetCreationTimestamp(FileSystemSessionItem item) { throw new NotImplementedException(); }

    protected override FileSystemStream DoGetMetadataStream(FileSystemSessionItem item, Action<FileSystemStream> disposeAction) { throw new NotImplementedException(); }

    protected override FileSystemStream DoGetPermissionsStream(FileSystemSessionItem item, Action<FileSystemStream> disposeAction) { throw new NotImplementedException(); }

    protected override bool DoGetReadOnly(FileSystemSessionItem item) { throw new NotImplementedException(); }

    protected override void DoSetReadOnly(FileSystemSessionItem item, bool readOnly) { throw new NotImplementedException(); }

    protected override void DoSetCreationTimestamp(FileSystemSessionItem item, DateTime timestamp) { throw new NotImplementedException(); }

    protected override void DoSetLastAccessTimestamp(FileSystemSessionItem item, DateTime timestamp) { throw new NotImplementedException(); }

    protected override void DoSetModificationTimestamp(FileSystemSessionItem item, DateTime timestamp) { throw new NotImplementedException(); }
  }
}
