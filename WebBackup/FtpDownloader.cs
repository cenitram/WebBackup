using FluentFTP;
using FluentFTP.Exceptions;
using Spectre.Console;

namespace WebBackup;

public class FtpDownloader(WebFtpSettings ftpSettings, string password) : IDisposable
{
    private readonly AsyncFtpClient _ftpClient = new(ftpSettings.Host, ftpSettings.Username, password);

    public async Task ConnectAsync(CancellationToken cancellationToken) => await _ftpClient.AutoConnect(cancellationToken);

    public async Task DownloadDirectoryAsync(IProgress<FtpProgress> progress, CancellationToken cancellationToken)
    {
        try
        {
            // download a folder and all its files, and delete extra files on disk
            await _ftpClient.DownloadDirectory(ftpSettings.LocalPath, ftpSettings.RemotePath, FtpFolderSyncMode.Mirror, progress: progress, token: cancellationToken);
        }
        catch (FtpAuthenticationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Authentication error:[/] {ex.Message}");
        }
        catch (FtpException ex)
        {
            AnsiConsole.MarkupLine($"[red]FTP error:[/] {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Operation was canceled.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An unexpected error occurred:[/] {ex.Message}");
        }
    }

    public async Task<int> GetNumberOfFilesInDirectoryAsync(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _ftpClient.GetListing(ftpSettings.RemotePath, FtpListOption.Recursive, cancellationToken);
            return items.Count(item => item.Type == FtpObjectType.File);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred while counting files:[/] {ex.Message}");
            return -1; // Indicate an error
        }
    }

    public static async Task<bool> CheckUserCredentialsAsync(WebFtpSettings ftpSettings, string password, CancellationToken cancellationToken)
    {
        try
        {
            using var ftp = new AsyncFtpClient(ftpSettings.Host, ftpSettings.Username, password);
            await ftp.AutoConnect(cancellationToken);
            return true; // Credentials are valid
        }
        catch (FtpAuthenticationException)
        {
            return false; // Authentication failed
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An unexpected error occurred while checking credentials:[/] {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        _ftpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
