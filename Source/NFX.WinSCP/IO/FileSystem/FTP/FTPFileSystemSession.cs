using NFX;
using NFX.Environment;

using WinSCP;

namespace NFX.IO.FileSystem.SFTP
{
  public class FTPFileSystemSessionConnectParams : FileSystemSessionConnectParams
  {
    public const string CONFIG_URL_ATTR = "server-url";
    public const string CONFIG_PROTOCOL_ATTR = "protocol";

    public FTPFileSystemSessionConnectParams(Protocol protocol = Protocol.Sftp) : base() { Options.Protocol = protocol; }
    public FTPFileSystemSessionConnectParams(IConfigSectionNode node) : base(node) { }
    public FTPFileSystemSessionConnectParams(string connectString, string format = Configuration.CONFIG_LACONIC_FORMAT)
      : base(connectString, format) { }

    internal readonly SessionOptions Options = new SessionOptions();

    public Protocol Protocol { get { return Options.Protocol; } }

    [Config] public string Host { get { return Options.HostName; }   set { Options.HostName = value; } }
    [Config] public int    Port { get { return Options.PortNumber; } set { Options.PortNumber = value; } }

    [Config] public string UserName { get { return Options.UserName; } set { Options.UserName = value; } }
    [Config] public string Password { get { return Options.Password; } set { Options.Password = value; } }

    [Config]
    public string Fingerprint
    {
      get { return Protocol == Protocol.Sftp || Protocol == Protocol.Scp ? Options.SshHostKeyFingerprint  : Options.TlsHostCertificateFingerprint; }
      set
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp)
          Options.SshHostKeyFingerprint = value;
        else Options.TlsHostCertificateFingerprint = value;
      }
    }

    [Config]
    public bool AcceptAny
    {
      get { return Protocol == Protocol.Sftp || Protocol == Protocol.Scp ? Options.GiveUpSecurityAndAcceptAnyTlsHostCertificate : Options.GiveUpSecurityAndAcceptAnySshHostKey; }
      set
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp)
          Options.GiveUpSecurityAndAcceptAnySshHostKey = value;
        else Options.GiveUpSecurityAndAcceptAnyTlsHostCertificate = value;
      }
    }

    [Config]
    public string PrivateKeyPath
    {
      get { return Protocol == Protocol.Sftp || Protocol == Protocol.Scp ? Options.SshPrivateKeyPath : Options.TlsClientCertificatePath; }
      set
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp)
          Options.SshPrivateKeyPath = value;
        else Options.TlsClientCertificatePath = value;
      }
    }

    [Config]
    public string PrivateKeyPassphrase { get { return Options.PrivateKeyPassphrase; } set { Options.PrivateKeyPassphrase = value; } }

    [Config]
    public int TimeoutMs { get { return Options.TimeoutInMilliseconds; } set { Options.TimeoutInMilliseconds = value < 0 ? 0 : value; } }

    [Config]
    public FtpSecure Secure
    {
      get
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp) return FtpSecure.Implicit;
        if (Protocol == Protocol.Webdav && Options.WebdavSecure) return FtpSecure.Implicit;
        return Options.FtpSecure;
      }
      set
      {
        if (Protocol == Protocol.Webdav) Options.WebdavSecure = value != FtpSecure.None;
        if (Protocol == Protocol.Ftp) Options.FtpSecure = value;
      }
    }

    [Config]
    public string RootPath { get { return Options.WebdavRoot; } set { Options.WebdavRoot = value; } }

    public override void Configure(IConfigSectionNode node)
    {
      var url = node.AttrByName(CONFIG_URL_ATTR);
      if (url.Exists)
        Options.ParseUrl(url.ValueAsString());

      var protocol = node.AttrByName(CONFIG_PROTOCOL_ATTR);
      if (protocol.Exists)
        Options.Protocol = protocol.ValueAsEnum(Protocol.Sftp);
      base.Configure(node);
    }
  }

  public class FTPFileSystemSession : FileSystemSession
  {
    public FTPFileSystemSession(FTPFileSystem fs, IFileSystemHandle handle, FTPFileSystemSessionConnectParams cParams)
      : base(fs, handle, cParams)
    {
      m_Connection = new Session();
      m_Connection.Open(cParams.Options);
    }

    private Session m_Connection;

    internal Session Connection { get { return m_Connection; } }

    protected override void Destructor()
    {
      DisposeAndNull(ref m_Connection);
      base.Destructor();
    }

    protected override void ValidateConnectParams(FileSystemSessionConnectParams cParams)
    {
      var sftpCParams = cParams as FTPFileSystemSessionConnectParams;

      if (sftpCParams == null)
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams(cParams=null|cParams.Type!=SFTPFileSystemSessionConnectParams)");

      if (sftpCParams.Host.IsNotNullOrWhiteSpace())
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams($host=null|empty");

      if (sftpCParams.UserName.IsNotNullOrWhiteSpace())
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams($user=null|empty");

      if (!sftpCParams.AcceptAny && sftpCParams.Fingerprint.IsNotNullOrWhiteSpace())
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams($fingerpring=null|empty");

      base.ValidateConnectParams(cParams);
    }
  }
}
