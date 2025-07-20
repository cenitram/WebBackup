namespace WebBackup;

[Obsolete("Use WebBackupSettings instead.")]
public record WebFtpSettings(string Name, string Host, string Username, string LocalPath, string RemotePath);