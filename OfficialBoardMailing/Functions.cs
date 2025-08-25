using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficialBoardMailing.Repositories;

namespace OfficialBoardMailing;

public class Functions
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly IOfficialBoardRepository _officialBoardRepository;
    private readonly IEmailSender _emailSender;

    public Functions(ILoggerFactory loggerFactory, IConfiguration config, IOfficialBoardRepository officialBoardRepository, IEmailSender emailSender)
    {
        _logger = loggerFactory.CreateLogger<Functions>();
        _config = config;
        _officialBoardRepository = officialBoardRepository;
        _emailSender = emailSender;

    }

    [Function("MailNewFilesInOfficialBoard")]
    public async Task Run([TimerTrigger("0 0 18 * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {DateTimeNow}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {NextSchedule}", myTimer.ScheduleStatus.Next);
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

            var unsentDocuments = _officialBoardRepository.GetUnsentDocuments().ToList();
            if (unsentDocuments.Count == 0)
            {
                _logger.LogInformation("No unsent documents found.");
                return;
            }

            // TODO: Provide recipients from your own source (e.g., database or app input)
            List<string> recipients = ["martinec98@gmail.com"];

            if (recipients.Count == 0)
            {
                _logger.LogWarning("No email recipients provided. Skipping email send and not marking documents as sent.");
                return;
            }

            await _emailSender.SendUnsentDocumentsAsync(unsentDocuments, recipients);

            _officialBoardRepository.MarkDocumentsAsSent(unsentDocuments.Select(d => d.Id));

            _logger.LogInformation("Official board files processed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error establishing SSH tunnel: {ExceptionMessage}", ex.Message);
        }
        finally
        {
            connector.Disconnect();
        }
    }
}
