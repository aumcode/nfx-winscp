using System;

using NFX;
using NFX.ApplicationModel;
using NFX.Environment;
using NFX.IO.FileSystem;

using NUnit.Framework;

namespace NFX.WinSCP.NUnit.Integration.IO.FileSystem.FTP
{
  [TestFixture]
  public class FTPFileSystemTests
  {
    protected string LACONF = typeof(FTPFileSystemTests).GetText("FTPFileSystemTests.laconf");

    private ConfigSectionNode m_Config;

    private ServiceBaseApplication m_App;

    [OneTimeSetUp]
    public void SetUp()
    {
      m_Config = LACONF.AsLaconicConfig(handling: ConvertErrorHandling.Throw);
      m_App = new ServiceBaseApplication(null, m_Config);
    }

    [OneTimeTearDown]
    public void TearDown() { DisposableObject.DisposeAndNull(ref m_App); }

    private IFileSystem m_FileSystem;
    public IFileSystem FileSystem
    {
      get
      {
        if (m_FileSystem == null) m_FileSystem = NFX.IO.FileSystem.FileSystem.Instances["sftp"];
        return m_FileSystem;
      }
    }

    [Test]
    public void Connect()
    {
      var remotePath = ".";
      var remotePathAttr = m_Config.Navigate("/tests/connect/$path");
      if (remotePathAttr.Exists)
        remotePath = remotePathAttr.ValueAsString();

      using (var session = FileSystem.StartSession(null))
      {
        var dir = session[remotePath] as FileSystemDirectory;
        Console.WriteLine("Files:");
        foreach (var item in dir.FileNames)
          Console.WriteLine("\t{0}", item);
        Console.WriteLine("Directories:");
        foreach (var item in dir.SubDirectoryNames)
          Console.WriteLine("\t{0}", item);
        var file = dir.CreateFile("nfx_ftp_fs.txt");
        file.WriteAllText("TEST");
        var remoteContent = file.ReadAllText();
        Console.WriteLine(remoteContent);

        Aver.AreEqual("TEST", remoteContent);
      }
    }
  }
}
