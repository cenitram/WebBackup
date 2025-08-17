using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OfficialBoardMailing;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendUnsentDocumentsAsync(IEnumerable<OfficialBoardModel> documents, IEnumerable<string> recipients, CancellationToken cancellationToken = default)
    {
        var docs = documents?.ToList() ?? new List<OfficialBoardModel>();
        if (docs.Count == 0)
        {
            _logger.LogInformation("No unsent documents to include in email. Skipping send.");
            return;
        }

        var toList = recipients?.Where(r => !string.IsNullOrWhiteSpace(r))
                                 .Select(r => r.Trim())
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToList() ?? new List<string>();
        if (toList.Count == 0)
        {
            _logger.LogWarning("Email recipients list is empty.");
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = $"Official board: {docs.Count} document(s) pending notification",
            Body = BuildHtmlBody(docs),
            IsBodyHtml = true
        };

        foreach (var r in toList)
            message.To.Add(r);

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.SmtpUser)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email with {Count} unsent document(s) sent to {RecipientCount} recipient(s).", docs.Count, toList.Count);
    }

    private static string BuildHtmlBody(List<OfficialBoardModel> docs)
    {
        var sb = new StringBuilder();
        sb.Append("<h3>Unsent official board documents</h3>");
        sb.Append("<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;'><thead><tr><th>ID</th><th>Document</th><th>Date</th><th>File</th><th>Description</th></tr></thead><tbody>");
        foreach (var d in docs)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{d.DokumentId}</td>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(d.Name)}</td>");
            sb.Append($"<td>{d.DateOfPosting:yyyy-MM-dd}</td>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode($"{d.FileName}.{d.FileExtension}")}</td>");
            sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(d.Description ?? string.Empty)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table><p>This is an automated message.</p>");
        return sb.ToString();
    }
}
