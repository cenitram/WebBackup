using FluentFTP;
using Spectre.Console;

namespace WebBackup;

public static class BackupManager
{
    public static async Task PerformBackup(List<(WebBackupSettings, string)> passwords, CancellationToken cancellationToken)
    {
        List<FtpDownloader> ftpDownloaders = [];
        try
        {
            await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    List<Task> tasks = [];
                    foreach ((var web, var password) in passwords)
                    {
                        var ftpDownloader = new FtpDownloader(web, password);
                        ftpDownloaders.Add(ftpDownloader);
                        await ftpDownloader.ConnectAsync(cancellationToken);

                        var filesInDirectory = await ftpDownloader.GetNumberOfFilesInDirectoryAsync(cancellationToken);
                        var task = ctx.AddTask($"{web.Name}", maxValue: filesInDirectory - 1);

                        var fileDownloadTask = ftpDownloader.DownloadDirectoryAsync(new Progress<FtpProgress>(p =>
                        {
                            task.Value = p.FileIndex;
                        }), cancellationToken);
                        tasks.Add(fileDownloadTask);
                    }
                    await Task.WhenAll(tasks).WaitAsync(cancellationToken);
                });
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Backup operation was canceled.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]An unexpected error occurred:[/] {ex.Message}");
        }
        finally
        {
            foreach (var ftpDownloader in ftpDownloaders)
                ftpDownloader.Dispose();
        }
    }
}
