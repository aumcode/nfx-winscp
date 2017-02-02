using System;

using NFX;
using NFX.Environment;

using WinSCP;

namespace NFX.IO.FileSystem.FTP
{
  public class FTPFileSystemSessionConnectParams : FileSystemSessionConnectParams
  {
    public const string CONFIG_RAW_SETTINGS_SECTION = "raw-settings";

    public const string CONFIG_SERVER_URL_ATTR = "server-url";
    public const string CONFIG_PROTOCOL_ATTR = "protocol";

    public FTPFileSystemSessionConnectParams(Protocol protocol = Protocol.Sftp) : base() { m_Options.Protocol = protocol; }
    public FTPFileSystemSessionConnectParams(IConfigSectionNode node) : base(node) { }
    public FTPFileSystemSessionConnectParams(string connectString, string format = Configuration.CONFIG_LACONIC_FORMAT)
      : base(connectString, format) { }

    private SessionOptions m_Options = new SessionOptions();

    public SessionOptions Options { get { return m_Options; } }

    public Protocol Protocol { get { return m_Options.Protocol; } }

    [Config] public string Host { get { return m_Options.HostName; }   set { m_Options.HostName = value; } }
    [Config] public int    Port { get { return m_Options.PortNumber; } set { m_Options.PortNumber = value; } }

    [Config] public string UserName { get { return m_Options.UserName; } set { m_Options.UserName = value; } }
    [Config] public string Password { get { return m_Options.Password; } set { m_Options.Password = value; } }

    [Config]
    public string Fingerprint
    {
      get { return (Protocol == Protocol.Sftp || Protocol == Protocol.Scp) ? m_Options.SshHostKeyFingerprint  : m_Options.TlsHostCertificateFingerprint; }
      set
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp)
          m_Options.SshHostKeyFingerprint = value;
        else m_Options.TlsHostCertificateFingerprint = value;
      }
    }

    [Config]
    public bool AcceptAny
    {
      get { return (Protocol == Protocol.Sftp || Protocol == Protocol.Scp) ? m_Options.GiveUpSecurityAndAcceptAnySshHostKey : m_Options.GiveUpSecurityAndAcceptAnyTlsHostCertificate; }
      set
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp)
          m_Options.GiveUpSecurityAndAcceptAnySshHostKey = value;
        else m_Options.GiveUpSecurityAndAcceptAnyTlsHostCertificate = value;
      }
    }

    [Config]
    public string PrivateKeyPath
    {
      get { return (Protocol == Protocol.Sftp || Protocol == Protocol.Scp) ? m_Options.SshPrivateKeyPath : m_Options.TlsClientCertificatePath; }
      set
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp)
          m_Options.SshPrivateKeyPath = value;
        else m_Options.TlsClientCertificatePath = value;
      }
    }

    [Config]
    public string PrivateKeyPassphrase { get { return m_Options.PrivateKeyPassphrase; } set { m_Options.PrivateKeyPassphrase = value; } }

    [Config]
    public int TimeoutMs { get { return m_Options.TimeoutInMilliseconds; } set { m_Options.TimeoutInMilliseconds = value < 0 ? 0 : value; } }

    [Config]
    public FtpSecure Secure
    {
      get
      {
        if (Protocol == Protocol.Sftp || Protocol == Protocol.Scp) return FtpSecure.Implicit;
        if (Protocol == Protocol.Webdav && m_Options.WebdavSecure) return FtpSecure.Implicit;
        return m_Options.FtpSecure;
      }
      set
      {
        if (Protocol == Protocol.Webdav) m_Options.WebdavSecure = value != FtpSecure.None;
        if (Protocol == Protocol.Ftp) m_Options.FtpSecure = value;
      }
    }

    [Config]
    public string RootPath { get { return m_Options.WebdavRoot; } set { m_Options.WebdavRoot = value; } }

    public override void Configure(IConfigSectionNode node)
    {
      m_Options = new SessionOptions();

      var serverUrlAttr = node.AttrByName(CONFIG_SERVER_URL_ATTR);
      if (serverUrlAttr.Exists)
      {
        var serverUrl = serverUrlAttr.ValueAsString();
        if (serverUrl.IsNotNullOrWhiteSpace())
          m_Options.ParseUrl(serverUrl);
      }

      var protocolAttr = node.AttrByName(CONFIG_PROTOCOL_ATTR);
      if (protocolAttr.Exists)
        m_Options.Protocol = protocolAttr.ValueAsEnum(Protocol.Sftp);

      base.Configure(node);

      var rawSettings = node[CONFIG_RAW_SETTINGS_SECTION];
      if (rawSettings.Exists)
        foreach (var attr in rawSettings.Attributes)
          m_Options.AddRawSettings(attr.Name, attr.ValueAsString());
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

      if (sftpCParams.Host.IsNullOrWhiteSpace())
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams($host=null|empty)");

      if (sftpCParams.UserName.IsNullOrWhiteSpace())
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams($user=null|empty)");

      if (!sftpCParams.AcceptAny && sftpCParams.Fingerprint.IsNullOrWhiteSpace())
        throw new NFXIOException(GetType().Name + ".ValidateConnectParams($fingerpring=null|empty)");

      base.ValidateConnectParams(cParams);
    }
  }
}
