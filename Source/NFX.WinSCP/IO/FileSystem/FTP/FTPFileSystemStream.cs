using System;
using System.IO;

using WinSCP;

namespace NFX.IO.FileSystem.FTP
{
  internal class FTPFileSystemStream : FileSystemStream
  {
    public FTPFileSystemStream(FileSystemFile file, Action<FileSystemStream> disposeAction) : base(file, disposeAction)
    {
      var session = Item.Session as FTPFileSystemSession;
      var handle = Item.Handle as FTPFileSystem.Handle;

      m_TempFile = Path.GetTempFileName();

      var options = new TransferOptions
      {
        TransferMode = TransferMode.Binary,
        OverwriteMode = OverwriteMode.Overwrite
      };

      var result = session.Connection.GetFiles(handle.FileInfo.FullName, m_TempFile, remove: false, options: options);
      result.Check();

      m_Stream = new FileStream(m_TempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
    }

    private string m_TempFile;
    private FileStream m_Stream;

    protected override void Dispose(bool disposing)
    {
      if (m_Stream != null)
      {
        m_Stream.Dispose();
        m_Stream = null;
      }
      base.Dispose(disposing);
      if (File.Exists(m_TempFile)) File.Delete(m_TempFile);
    }

    protected override void DoFlush()
    {
      m_Stream.Flush();

      var session = Item.Session as FTPFileSystemSession;
      var handle = Item.Handle as FTPFileSystem.Handle;
      var options = new TransferOptions
      {
        TransferMode = TransferMode.Binary,
        OverwriteMode = OverwriteMode.Overwrite
      };

      var result = session.Connection.PutFiles(m_TempFile, handle.FileInfo.FullName, options: options);
      result.Check();
    }

    protected override long DoGetLength() { return m_Stream.Length; }

    protected override long DoGetPosition() { return m_Stream.Position; }

    protected override int DoRead(byte[] buffer, int offset, int count) { return m_Stream.Read(buffer, offset, count); }

    protected override long DoSeek(long offset, SeekOrigin origin) { return m_Stream.Seek(offset, origin); }

    protected override void DoSetLength(long value) { m_Stream.SetLength(value); }

    protected override void DoSetPosition(long position) { m_Stream.Position = position; }

    protected override void DoWrite(byte[] buffer, int offset, int count) { m_Stream.Write(buffer, offset, count); }
  }
}
