using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficialBoardMailing.Repositories;

namespace OfficialBoardMailing;

public class Functions(
    ILoggerFactory loggerFactory,
    IConfiguration config,
    IOfficialBoardRepository officialBoardRepository,
    IRecipientsRepository recipientsRepository,
    IEmailSender emailSender)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Functions>();

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
            Host = config["SshHost"] ?? string.Empty,
            User = config["SshUser"] ?? string.Empty,
            Password = config["SshPassword"] ?? string.Empty
        };

        var connector = new SshConnector();
        try
        {
            connector.Connect(sshConfig);
            _logger.LogInformation("SSH tunnel with port forwarding established successfully.");

            var unsentDocuments = officialBoardRepository.GetUnsentDocuments();
            if (!unsentDocuments.Any())
            {
                _logger.LogInformation("No unsent documents found.");
                return;
            }

            // TODO: Provide recipients from your own source (e.g., database or app input)
            var recipients = recipientsRepository.GetRecipientsEmails();

            if (!recipients.Any())
            {
                _logger.LogWarning("No email recipients provided. Skipping email send and not marking documents as sent.");
                return;
            }

            await emailSender.SendUnsentDocumentsAsync(unsentDocuments, recipients);

            officialBoardRepository.MarkDocumentsAsSent(unsentDocuments.Select(d => d.Id));

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
