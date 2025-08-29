using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using RazorLight;
using System.Net;
using System.Net.Mail;
using System.Text;

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

    public async Task SendTestEmailAsync(CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, "Úřední deska Tlumačov"),
            Subject = "Test email from Azure Function",
            Body = "Hello from azure function",
            IsBodyHtml = false
        };
        message.To.Add("martinec98@gmail.com");

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Test email sent to {Recipient}.", "martinec98@gmail.com");
    }

    private static string BuildHtmlBody(List<OfficialBoardModel> docs)
    {
        var sb = new StringBuilder();
        sb.Append("<h3>Nové dokumenty na úřední desce obce Tlumačov</h3>");
        sb.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;'><thead><tr><th>Document</th><th>Date</th><th>File</th><th>Description</th></tr></thead><tbody>");
        foreach (var d in docs)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{WebUtility.HtmlEncode(d.Name)}</td>");
            sb.Append($"<td>{d.DateOfPosting:yyyy-MM-dd}</td>");
            var fileBase = $"{d.FileName}.{d.FileExtension}";
            var url = $"https://www.tlumacov.cz/wp-content/plugins/uredni-deska/dokumenty/{Uri.EscapeDataString(fileBase)}";
            sb.Append($"<td><a href=\"{url}\" target=\"_blank\" rel=\"noopener noreferrer\">{WebUtility.HtmlEncode(fileBase)}</a></td>");
            sb.Append($"<td>{WebUtility.HtmlEncode(d.Description ?? string.Empty)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table><p>This is an automated message.</p>");
        return sb.ToString();
    }
}
