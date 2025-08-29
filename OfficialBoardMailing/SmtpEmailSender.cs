using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using RazorLight;
using System.Net;
using System.Net.Mail;

namespace OfficialBoardMailing;

public class SmtpEmailSender(
    IMailPoetLinkService mailPoetLinkService,
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendUnsentDocumentsAsync(
        IEnumerable<OfficialBoardModel> documents,
        IEnumerable<RecipientsModel> recipients,
        CancellationToken cancellationToken = default)
    {
        var docs = documents?.ToList() ?? [];
        if (docs.Count == 0)
        {
            logger.LogInformation("No unsent documents to include in email. Skipping send.");
            return;
        }

        var recipientsList = recipients?.Where(r => !string.IsNullOrWhiteSpace(r.Email))
                                 .ToList() ?? [];

        if (recipientsList.Count == 0)
        {
            logger.LogWarning("Email recipients list is empty.");
            return;
        }

        // Build engine & compile template once for this batch
        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(AppContext.BaseDirectory, "Templates"))
            .UseMemoryCachingProvider()
            .Build();
        var compiledTemplate = await engine.CompileTemplateAsync("EmailTemplate.cshtml");

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        foreach (var recipient in recipientsList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var model = new EmailTemplateModel
            {
                Date = DateTime.Now,
                BoardDocuments = docs,
                SubscriptionManagementLink = mailPoetLinkService.CreateManageSubscriptionLink(recipient),
                UnsubscribeLink = mailPoetLinkService.CreateUnsubscribeLink(recipient),
            };

            var body = await engine.RenderTemplateAsync(compiledTemplate, model);

            using var message = new MailMessage
            {
                From = new MailAddress(_options.From, _options.DisplayName),
                Subject = $"Nové dokumenty na Úřední desce Tlumačova ze dne {DateTime.Now:dd.MM.yyyy}",
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(recipient.Email);

            await client.SendMailAsync(message, cancellationToken);
        }

        logger.LogInformation("Finished sending {Sent} personalized email(s) with {DocCount} document(s).", recipients.Count(), docs.Count);
    }
}
