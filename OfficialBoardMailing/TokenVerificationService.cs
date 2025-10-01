using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OfficialBoardMailing;

public class TokenVerificationService(ITokenService tokenService)
{
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

            // Validate the token signature using the same secret key from TokenService
            using var hmac = new HMACSHA256(tokenService.GetSecretKey());
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