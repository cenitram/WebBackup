using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OfficialBoardMailing;

public class Functions
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly IOfficialBoardRepository _officialBoardRepository;

    public Functions(ILoggerFactory loggerFactory, IConfiguration config, IOfficialBoardRepository officialBoardRepository)
    {
        _logger = loggerFactory.CreateLogger<Functions>();
        _config = config;
        _officialBoardRepository = officialBoardRepository;

    }

    [Function("MailNewFilesInOfficialBoard")]
    public void Run([TimerTrigger("0 0 18 * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }

        // Read SSH config as object from local.settings.json
        var sshConfig = new SshConfig
        {
            Host = _config["SshHost"] ?? string.Empty,
            User = _config["SshUser"] ?? string.Empty,
            Password = _config["SshPassword"] ?? string.Empty
        };

        var connector = new SshConnector();
        try
        {
            connector.Connect(sshConfig);
            _logger.LogInformation("SSH tunnel with port forwarding established successfully.");
            // Add further logic here if needed

            _officialBoardRepository.GetUnsentDocuments();

            _logger.LogInformation("Official board files processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error establishing SSH tunnel: {ex.Message}");
        }
        finally
        {
            connector.Disconnect();
        }
    }
}
