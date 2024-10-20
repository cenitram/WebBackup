using Microsoft.Extensions.Configuration;

namespace WebBackup
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var config = LoadConfiguration();
            var webSettings = config.GetSection("WebFtpSettings").Get<WebFtpSettings[]>() ?? [];
            var userSelectedWebs = UserInteraction.PromptUserForWebSelection(webSettings);
            using CancellationTokenSource tokenSource = new();
            Console.CancelKeyPress += (s, e) =>
            {
                tokenSource.Cancel();
                e.Cancel = true;
            };

            var websWithPassword = await UserInteraction.CollectPasswordsAsync(userSelectedWebs, tokenSource.Token);

            await BackupManager.PerformBackup(websWithPassword, tokenSource.Token);

            UserInteraction.DisplayBackupStatus(websWithPassword, tokenSource.IsCancellationRequested);
        }

        private static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json")
            .AddUserSecrets<Program>()
            .Build();
    }
}
