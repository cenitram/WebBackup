using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Tlumacov.OfficialBoard.Web.Services;

public class TokenVerifier(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<TokenVerificationResult> VerifyTokenAsync(string token)
    {
        try
        {
            // Split the token to get the payload part
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token format is invalid"
                };
            }

            // Decode the payload
            string base64Payload = parts[0];
            byte[] payloadBytes;

            try
            {
                payloadBytes = Convert.FromBase64String(base64Payload);
            }
            catch
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token payload is corrupted"
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

            // Verify token with backend
            var response = await _httpClient.PostAsJsonAsync("api/subscription/verify-token", new { Token = token });

            if (response.IsSuccessStatusCode)
            {
                return new TokenVerificationResult
                {
                    IsValid = true,
                    Message = "Subscription confirmed successfully",
                    Email = tokenData.Email
                };
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = $"Server verification failed: {errorMessage}",
                    Email = tokenData.Email
                };
            }
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

    // Local validation only for client-side preview
    public TokenVerificationResult ValidateTokenLocally(string token)
    {
        try
        {
            // Split the token to get the payload part
            var parts = token.Split('.');
            if (parts.Length != 2)
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token format is invalid"
                };
            }

            // Decode the payload
            string base64Payload = parts[0];
            byte[] payloadBytes;

            try
            {
                payloadBytes = Convert.FromBase64String(base64Payload);
            }
            catch
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Message = "Token payload is corrupted"
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

            // In client-side Blazor we can only do basic validation
            // A full verification would require the secret key which should not be exposed to the client
            return new TokenVerificationResult
            {
                IsValid = true,
                Message = "Token appears valid (Note: Full verification requires server-side validation)",
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