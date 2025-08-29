using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using OfficialBoardMailing.Repositories;

namespace OfficialBoardMailing;

public class Functions(
    ILoggerFactory loggerFactory,
    IOfficialBoardRepository officialBoardRepository,
    IRecipientsRepository recipientsRepository,
    IEmailSender emailSender,
    IOptions<SshOptions> sshOptions)
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
        var connector = new SshConnector();
        try
        {
            connector.Connect(sshOptions.Value);
            _logger.LogInformation("SSH tunnel with port forwarding established successfully.");

            var unsentDocuments = officialBoardRepository.GetUnsentDocuments();
            if (!unsentDocuments.Any())
            {
                _logger.LogInformation("No unsent documents found.");
                return;
            }

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
