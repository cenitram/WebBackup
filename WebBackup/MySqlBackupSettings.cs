namespace WebBackup;

public record MySqlBackupSettings(
    string Name,
    string SshHost,
    int SshPort,
    string SshUsername,
    string SshPassword,
    string MySqlHost,
    int MySqlPort,
    string MySqlUsername,
    string Database,
    string LocalBackupPath
);
