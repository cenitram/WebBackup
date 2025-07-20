using MySql.Data.MySqlClient;
using Renci.SshNet;
using Spectre.Console;

namespace WebBackup;

public static class MySqlBackupManager
{
    public static async Task PerformMySqlBackup(WebBackupSettings settings, string mysqlPassword, CancellationToken cancellationToken)
    {
        // SSH tunnel setup
        using var client = new SshClient(settings.SshHost, settings.SshPort, settings.SshUsername, settings.SshPassword);
        client.Connect();
        if (!client.IsConnected)
        {
            AnsiConsole.MarkupLine($"[red]SSH connection failed to {settings.SshHost}[/]");
            return;
        }

        using var portForward = new ForwardedPortLocal("127.0.0.1", (uint)settings.MySqlPort, settings.MySqlHost, (uint)settings.MySqlPort);
        client.AddForwardedPort(portForward);
        portForward.Start();

        try
        {
            string dumpFile = Path.Combine(settings.MySqlLocalBackupPath, $"{settings.Database}_{DateTime.Now:yyyyMMdd_HHmmss}.sql");
            Directory.CreateDirectory(settings.MySqlLocalBackupPath);

            string connStr = $"server=127.0.0.1;port={settings.MySqlPort};user={settings.MySqlUsername};password={mysqlPassword};database={settings.Database};SslMode=none;convertzerodatetime=true;";
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync(cancellationToken);

            using var cmd = new MySqlCommand();
            cmd.Connection = conn;

            using (var mb = new MySqlBackup(cmd))
            {
                mb.ExportInfo.InsertLineBreakBetweenInserts = true;
                await Task.Run(() => mb.ExportToFile(dumpFile), cancellationToken);
            }

            AnsiConsole.MarkupLine($"[green]MySQL backup completed: {dumpFile}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]MySQL backup error:[/] {ex.Message}");
        }
        finally
        {
            portForward.Stop();
            client.Disconnect();
        }
    }
}