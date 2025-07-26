using Renci.SshNet;
using MySqlConnector;
using System.Data;

namespace OfficialBoardMailing;

public class SshMySqlConnector
{
    private SshClient? _sshClient;
    private ForwardedPortLocal? _portForwarded;
    private MySqlConnection? _connection;

    public void Connect(SshConfig sshConfig, string connectionString)
    {
        // Establish SSH tunnel
        _sshClient = new SshClient(sshConfig.Host, sshConfig.User, sshConfig.Password);
        _sshClient.Connect();
        _portForwarded = new ForwardedPortLocal("127.0.0.1", 3306, "127.0.0.1", 3306);
        _sshClient.AddForwardedPort(_portForwarded);
        _portForwarded.Start();

        // Connect to MySQL
        _connection = new MySqlConnection(connectionString);
        _connection.Open();
    }

    public DataTable ExecuteQuery(string query)
    {
        if (_connection == null)
            throw new InvalidOperationException("Not connected to database.");
        using var cmd = new MySqlCommand(query, _connection);
        using var adapter = new MySqlDataAdapter(cmd);
        var dt = new DataTable();
        adapter.Fill(dt);
        return dt;
    }

    public void Disconnect()
    {
        _connection?.Close();
        _portForwarded?.Stop();
        _sshClient?.Disconnect();
    }
}
