using Microsoft.Extensions.Configuration;

namespace WebBackup;

public class Program
{
    static async Task Main(string[] args)
    {
        var config = LoadConfiguration();
        var webBackupSettings = config.GetSection("WebBackupSettings").Get<WebBackupSettings[]>() ?? [];
        var userSelectedWebs = UserInteraction.PromptUserForWebSelection(webBackupSettings);
        using CancellationTokenSource tokenSource = new();
        Console.CancelKeyPress += (s, e) =>
        {
            tokenSource.Cancel();
            e.Cancel = true;
        };


        // Prompt for MySQL backups
        foreach (var web in userSelectedWebs)
        {
            var mysqlPassword = UserInteraction.PromptForMySqlPassword(web.Name);
            await MySqlBackupManager.PerformMySqlBackup(web, mysqlPassword, tokenSource.Token);
        }

        var websWithPassword = await UserInteraction.CollectPasswordsAsync(userSelectedWebs, tokenSource.Token);
        await BackupManager.PerformBackup(websWithPassword, tokenSource.Token);

        UserInteraction.DisplayBackupStatus(websWithPassword, tokenSource.IsCancellationRequested);
    }

    private static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
        .AddJsonFile($"appsettings.json")
        .AddUserSecrets<Program>()
        .Build();
}
