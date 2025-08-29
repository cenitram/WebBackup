using Microsoft.Extensions.Options;
using OfficialBoardMailing.Options;
using System.Text.Json;

namespace OfficialBoardMailing;

public class MailPoetLinkService(IOptions<MailPoetLinkOptions> mailPoetLinkOptions) : IMailPoetLinkService
{
    private const string BaseUrl = "https://www.tlumacov.cz/?mailpoet_router&endpoint=track&action=click&data=";
    private readonly MailPoetLinkOptions _options = mailPoetLinkOptions.Value;

    public string CreateManageSubscriptionLink(RecipientsModel recipient)
    {
        var data = new object[] { recipient.Id.ToString(), recipient.LinkToken, _options.QueueId, _options.ManageSubscriptionHash, false };
        return CreateLink(data);
    }

    public string CreateUnsubscribeLink(RecipientsModel recipient)
    {
        var data = new object[] { recipient.Id.ToString(), recipient.LinkToken, _options.QueueId, _options.UnsubscribeHash, false };
        return CreateLink(data);
    }


    private string CreateLink(object[] data)
    {
        var serializedData = JsonSerializer.Serialize(data);
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(serializedData));

        return BaseUrl + Uri.EscapeDataString(base64Data);
    }
}
