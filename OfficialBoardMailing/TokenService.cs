using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OfficialBoardMailing;

public class TokenService : ITokenService
{
    private readonly EmailOptions _options;

    public TokenService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string CreateConfirmationToken(string email, TimeSpan? expirationTime = null)
    {
        // Default expiration is 24 hours
        var expiration = expirationTime ?? TimeSpan.FromHours(24);
        
        // Create a payload with subscriber information and expiration
        var tokenData = new
        {
            Email = email,
            Timestamp = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration),
            TokenId = Guid.NewGuid().ToString()
        };

        return CreateToken(tokenData);
    }

    public string CreateUnsubscribeToken(RecipientsModel recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        
        // Create data object with recipient information
        var tokenData = new
        {
            Email = recipient.Email,
            Id = recipient.Id,
            Token = recipient.UnsubscribeToken,
            Timestamp = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddYears(1) // Unsubscribe tokens can last longer
        };

        return CreateToken(tokenData);
    }

    public byte[] GetSecretKey()
    {
        string secretKey = _options.TokenSecret ?? "YourVerySecretKeyForTokenGeneration-ShouldBeAtLeast32CharsLong";
        return Encoding.UTF8.GetBytes(secretKey);
    }
    
    private string CreateToken(object tokenData)
    {
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
}