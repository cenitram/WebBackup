namespace WebBackup;

public record WebBackupSettings(
    string Name,
    string Host,
    string Username,
    string LocalPath,
    string RemotePath,
    string SshHost,
    int SshPort,
    string SshUsername,
    string SshPassword,
    string MySqlHost,
    int MySqlPort,
    string MySqlUsername,
    string Database,
    string MySqlLocalBackupPath
);
