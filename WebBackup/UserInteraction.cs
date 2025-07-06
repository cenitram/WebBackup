using Spectre.Console;

namespace WebBackup;

public static class UserInteraction
{
    public static List<WebFtpSettings> PromptUserForWebSelection(WebFtpSettings[] webSettings) 
        => AnsiConsole.Prompt(
            new MultiSelectionPrompt<WebFtpSettings>()
                .Title("Which webs do you want to backup?")
                .NotRequired()
                .InstructionsText(
                    "[grey](Press [blue]<space>[/] to toggle a web, " +
                    "[green]<enter>[/] to accept)[/]")
                .AddChoices(webSettings)
                .UseConverter(webSetting => webSetting.Name));

    public static async Task<List<(WebFtpSettings, string)>> CollectPasswordsAsync(IEnumerable<WebFtpSettings> userSelectedWebs, CancellationToken cancellationToken)
    {
        List<(WebFtpSettings, string)> passwords = [];
        foreach (var web in userSelectedWebs)
        {
            bool isAuthenticated = false;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var password = AnsiConsole.Prompt(
                    new TextPrompt<string>($"Enter FTP password for {web.Name}:")
                        .Secret());

                if (await FtpDownloader.CheckUserCredentialsAsync(web, password, cancellationToken))
                {
                    passwords.Add((web, password));
                    isAuthenticated = true;
                    break;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Invalid credentials for {web.Name}. Please try again.[/]");
                }
            }

            if (!isAuthenticated)
                AnsiConsole.MarkupLine($"[red]Failed to authenticate {web.Name} after 3 attempts. Please check also Username field in appsettings.json. Skipping...[/]");
        }
        return passwords;
    }

    public static void DisplayBackupStatus(List<(WebFtpSettings web, string password)> passwords, bool isCancellationRequested)
    {
        foreach (var (web, _) in passwords)
        {
            if (isCancellationRequested)
                Console.WriteLine($"Backup for {web.Name} was canceled. Already downloaded files can be found here:");
            else
                Console.WriteLine($"Backup for {web.Name} can be found here:");

            var path = new TextPath($"{web.LocalPath}")
                .RootStyle(new Style(foreground: Color.Blue))
                .SeparatorStyle(new Style(foreground: Color.Blue))
                .StemStyle(new Style(foreground: Color.Blue))
                .LeafStyle(new Style(foreground: Color.Blue));
            AnsiConsole.Write(path);
            AnsiConsole.WriteLine();
        }
    }
}
