using System.Net;
using Tlumacov.OfficialBoard.Shared.Models;

namespace Tlumacov.OfficialBoard.Shared;

public class MailPoetLinkService(ITokenService tokenService) : IMailPoetLinkService
{
    private const string BaseUrl = "https://localhost:7181/unsubscribe?token=";

    public string CreateUnsubscribeLink(RecipientsModel recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        // Create a token using the recipient's UnsubscribeToken
        string token = tokenService.CreateUnsubscribeToken(recipient);

        // Return the complete URL with the encoded token
        return $"{BaseUrl}{WebUtility.UrlEncode(token)}";
    }
}
