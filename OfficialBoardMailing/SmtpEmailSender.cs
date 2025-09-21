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
        var docs = NormalizeDocuments(documents);
        if (docs.Count == 0)
        {
            logger.LogInformation("No unsent documents to include in email. Skipping send.");
            return;
        }

        var recipientsList = NormalizeRecipients(recipients);
        if (recipientsList.Count == 0)
        {
            logger.LogWarning("Email recipients list is empty.");
            return;
        }

        // Prepare reusable resources
        var (engine, compiledTemplate) = await PrepareTemplateAsync();
        using var client = CreateSmtpClient();

        foreach (var recipient in recipientsList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await SendEmailAsync(client, engine, compiledTemplate, docs, recipient, cancellationToken);
        }

        logger.LogInformation("Finished sending {Sent} personalized email(s) with {DocCount} document(s).", recipientsList.Count, docs.Count);
    }

    public async Task SendConfirmationEmailAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        // In a real implementation, generate a secure token and store it for verification
        var confirmationLink = $"https://example.com/confirm?email={WebUtility.UrlEncode(toEmail)}&token=dummy-token";
        var subject = "Potvrďte svůj odběr oznámení";
        var body = $@"Děkujeme za registraci k odběru oznámení úřední desky.\n\nProsím potvrďte svůj odběr kliknutím na následující odkaz: <a href='{confirmationLink}'>Potvrdit odběr</a>";

        using var client = CreateSmtpClient();
        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, _options.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(toEmail));
        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Confirmation email sent to {Email}", toEmail);
    }

    // Helper methods
    private static List<OfficialBoardModel> NormalizeDocuments(IEnumerable<OfficialBoardModel> documents) => documents?.ToList() ?? [];

    private static List<RecipientsModel> NormalizeRecipients(IEnumerable<RecipientsModel> recipients) =>
        recipients?.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList() ?? [];

    private async Task<(RazorLightEngine engine, ITemplatePage compiledTemplate)> PrepareTemplateAsync()
    {
        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(AppContext.BaseDirectory, "Templates"))
            .UseMemoryCachingProvider()
            .Build();

        var compiledTemplate = await engine.CompileTemplateAsync("EmailTemplate.cshtml");
        return (engine, compiledTemplate);
    }

    private SmtpClient CreateSmtpClient() => new(_options.SmtpHost, _options.SmtpPort)
    {
        EnableSsl = _options.UseSsl,
        Credentials = string.IsNullOrWhiteSpace(_options.SmtpUser)
            ? CredentialCache.DefaultNetworkCredentials
            : new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
    };

    private EmailTemplateModel BuildEmailModel(List<OfficialBoardModel> docs, RecipientsModel recipient) => new()
    {
        Date = DateTime.Now,
        BoardDocuments = docs,
        SubscriptionManagementLink = mailPoetLinkService.CreateManageSubscriptionLink(recipient),
        UnsubscribeLink = mailPoetLinkService.CreateUnsubscribeLink(recipient),
    };

    private MailMessage CreateMailMessage(string recipientEmail, string body) => new()
    {
        From = new MailAddress(_options.From, _options.DisplayName),
        Subject = $"Nové dokumenty na úřední desce Obce Tlumačov ze dne {DateTime.Now:dd.MM.yyyy}",
        Body = body,
        IsBodyHtml = true,
        To = { new MailAddress(recipientEmail) }
    };

    private async Task SendEmailAsync(SmtpClient client, RazorLightEngine engine, ITemplatePage compiledTemplate, List<OfficialBoardModel> docs, RecipientsModel recipient, CancellationToken cancellationToken)
    {
        var model = BuildEmailModel(docs, recipient);
        var body = await engine.RenderTemplateAsync(compiledTemplate, model);

        using var message = CreateMailMessage(recipient.Email, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
