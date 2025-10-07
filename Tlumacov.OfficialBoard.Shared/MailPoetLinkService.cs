using Microsoft.Extensions.Options;
using System.Net;
using Tlumacov.OfficialBoard.Shared.Models;
using Tlumacov.OfficialBoard.Shared.Options;

namespace Tlumacov.OfficialBoard.Shared;

public class MailPoetLinkService(ITokenService tokenService, IOptions<EmailOptions> options) : IMailPoetLinkService
{
    private readonly EmailOptions _options = options.Value;

    public string CreateUnsubscribeLink(RecipientsModel recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        // Create a token using the recipient's UnsubscribeToken
        string token = tokenService.CreateUnsubscribeToken(recipient);

        // Return the complete URL with the encoded token
        return $"{_options.WebApplicationUrl}/unsubscribe?token={WebUtility.UrlEncode(token)}";
    }
}
