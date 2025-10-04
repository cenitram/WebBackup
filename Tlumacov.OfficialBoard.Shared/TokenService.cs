using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tlumacov.OfficialBoard.Shared.Models;
using Tlumacov.OfficialBoard.Shared.Options;

namespace Tlumacov.OfficialBoard.Shared;

public class TokenService(IOptions<EmailOptions> options) : ITokenService
{
    private readonly EmailOptions _options = options.Value;

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

    public TokenVerificationResult VerifyToken(string token)
    {
        try
        {
            // Split the token to get the payload and signature parts
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token format is invalid"
                };
            }

            string base64Payload = parts[0];
            string base64Signature = parts[1];

            byte[] payloadBytes;
            byte[] signatureBytes;

            try
            {
                payloadBytes = Convert.FromBase64String(base64Payload);
                signatureBytes = Convert.FromBase64String(base64Signature);
            }
            catch
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token payload or signature is corrupted"
                };
            }

            // Parse the JSON payload
            string jsonPayload = Encoding.UTF8.GetString(payloadBytes);
            var tokenData = JsonSerializer.Deserialize<TokenData>(jsonPayload);

            if (tokenData == null)
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Could not parse token data"
                };
            }

            // Check if token is expired
            if (tokenData.ExpiresAt < DateTime.UtcNow)
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token has expired",
                    Email = tokenData.Email
                };
            }

            // Validate the token signature using the secret key
            using var hmac = new HMACSHA256(GetSecretKey());
            byte[] computedSignature = hmac.ComputeHash(payloadBytes);

            // Compare computed signature with received signature
            if (!CompareSignatures(computedSignature, signatureBytes))
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token signature is invalid",
                    Email = tokenData.Email
                };
            }

            return new TokenVerificationResult
            {
                IsValid = true,
                Message = "Subscription confirmed successfully",
                Email = tokenData.Email
            };
        }
        catch (Exception ex)
        {
            return new TokenVerificationResult
            {
                IsValid = false,
                Message = $"An error occurred: {ex.Message}"
            };
        }
    }

    // Constant-time comparison to prevent timing attacks
    private static bool CompareSignatures(byte[] signature1, byte[] signature2)
    {
        if (signature1.Length != signature2.Length)
        {
            return false;
        }

        int result = 0;
        for (int i = 0; i < signature1.Length; i++)
        {
            // XOR each byte - if they're identical, XOR will be 0
            // Using OR to accumulate any non-zero results
            result |= signature1[i] ^ signature2[i];
        }

        // If all bytes matched, result will be 0
        return result == 0;
    }
}

public class TokenVerificationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class TokenData
{
    public string Email { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenId { get; set; } = string.Empty;
}

public class VerifyTokenRequest
{
    public string Token { get; set; } = string.Empty;
}