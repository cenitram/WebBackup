using Tlumacov.OfficialBoard.Shared.Models;

namespace Tlumacov.OfficialBoard.Shared;

public interface ITokenService
{
    /// <summary>
    /// Creates a security token for email subscription confirmation
    /// </summary>
    /// <param name="email">The email address to include in the token</param>
    /// <param name="expirationTime">Optional expiration time, defaults to 24 hours</param>
    /// <returns>A signed JWT-style token</returns>
    string CreateConfirmationToken(string email, TimeSpan? expirationTime = null);

    /// <summary>
    /// Creates a security token for unsubscription
    /// </summary>
    /// <param name="recipient">The recipient model containing the necessary data</param>
    /// <returns>A signed JWT-style token</returns>
    string CreateUnsubscribeToken(RecipientsModel recipient);

    /// <summary>
    /// Gets the configured secret key as bytes
    /// </summary>
    /// <returns>Secret key bytes</returns>
    byte[] GetSecretKey();

    /// <summary>
    /// Verifies a token's validity and returns the verification result
    /// </summary>
    /// <param name="token">The token to verify</param>
    /// <returns>A result object containing verification status and information</returns>
    TokenVerificationResult VerifyToken(string token);
}