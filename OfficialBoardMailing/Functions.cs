using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace OfficialBoardMailing;

public class Functions
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    public Functions(ILoggerFactory loggerFactory, IConfiguration config)
    {
        _logger = loggerFactory.CreateLogger<Functions>();
        _config = config;
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
        var sshSection = _config.GetSection("SshConfig");
        var sshConfig = new SshConfig
        {
            Host = sshSection["Host"] ?? string.Empty,
            User = sshSection["User"] ?? string.Empty,
            Password = sshSection["Password"] ?? string.Empty
        };
        var connectionString = _config["ConnectionString"];

        var connector = new SshMySqlConnector();
        try
        {
            connector.Connect(
                sshConfig: sshConfig,
                connectionString: connectionString
            );
            _logger.LogInformation("Connected to MySQL over SSH successfully.");
            // Example query
            var result = connector.ExecuteQuery("SELECT NOW() AS CurrentTime;");
            _logger.LogInformation($"Query result: {result.Rows[0]["CurrentTime"]}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error connecting to MySQL over SSH: {ex.Message}");
        }
        finally
        {
            connector.Disconnect();
        }
    }
}
