using Microsoft.Extensions.Configuration;

namespace WebBackup
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var config = LoadConfiguration();
            var webSettings = config.GetSection("WebFtpSettings").Get<WebFtpSettings[]>() ?? [];
        }

        private static IConfiguration LoadConfiguration() => new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json")
            .AddUserSecrets<Program>()
            .Build();
    }
}
