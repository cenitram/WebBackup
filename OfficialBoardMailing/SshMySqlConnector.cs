using OfficialBoardMailing.Options;
using Renci.SshNet;

namespace OfficialBoardMailing;

public class SshConnector
{
    private SshClient? _sshClient;
    private ForwardedPortLocal? _portForwarded;

    public bool IsConnected => _sshClient?.IsConnected ?? false;

    public void Connect(SshOptions sshConfig, int localPort = 3306, int remotePort = 3306)
    {
        // Establish SSH tunnel with port forwarding
        _sshClient = new SshClient(sshConfig.Host, sshConfig.User, sshConfig.Password);
        _sshClient.Connect();
        _portForwarded = new ForwardedPortLocal("127.0.0.1", (uint)localPort, "127.0.0.1", (uint)remotePort);
        _sshClient.AddForwardedPort(_portForwarded);
        _portForwarded.Start();
    }

    public void Disconnect()
    {
        _portForwarded?.Stop();
        _sshClient?.Disconnect();
    }
}
