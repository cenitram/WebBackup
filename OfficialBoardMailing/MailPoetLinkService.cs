using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OfficialBoardMailing;

public class MailPoetLinkService(IOptions<EmailOptions> options) : IMailPoetLinkService
{
    private const string BaseUrl = "https://localhost:7181/unsubscribe?token=";
    private readonly EmailOptions _options = options.Value;

    public string CreateUnsubscribeLink(RecipientsModel recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        // Create a token using the recipient's UnsubscribeToken
        string token = CreateUnsubscribeToken(recipient);

        // Return the complete URL with the encoded token
        return $"{BaseUrl}{WebUtility.UrlEncode(token)}";
    }

    private string CreateUnsubscribeToken(RecipientsModel recipient)
    {
        // Create data object with recipient information
        var tokenData = new
        {
            Email = recipient.Email,
            Id = recipient.Id,
            Token = recipient.UnsubscribeToken,
            Timestamp = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1) // Unsubscribe tokens can last longer
        };

        // Serialize to JSON
        string jsonPayload = JsonSerializer.Serialize(tokenData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

        // Create a signature using HMAC
        using var hmac = new HMACSHA256(GetSecretKey());
        byte[] signatureBytes = hmac.ComputeHash(payloadBytes);

        // Encode the payload and signature
        string base64Payload = Convert.ToBase64String(payloadBytes);
        string base64Signature = Convert.ToBase64String(signatureBytes);

        // Combine into token format: payload.signature
        return $"{base64Payload}.{base64Signature}";
    }

    private byte[] GetSecretKey()
    {
        // Use the same secret key as in TokenVerificationService and SmtpEmailSender
        string secretKey = _options.TokenSecret ?? "YourVerySecretKeyForTokenGeneration-ShouldBeAtLeast32CharsLong";
        return Encoding.UTF8.GetBytes(secretKey);
    }
}
