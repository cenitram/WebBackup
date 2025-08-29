using System.Text.Json;

namespace OfficialBoardMailing;

public class MailPoetLinkService : IMailPoetLinkService
{
    private const string BaseUrl = "https://www.tlumacov.cz/?mailpoet_router&endpoint=track&action=click&data=";

    public string CreateManageSubscriptionLink(RecipientsModel recipient)
    {
        var data = new object[] { recipient.Id.ToString(), recipient.LinkToken, "3901", "819f11ae624b", false };
        return CreateLink(data);
    }

    public string CreateUnsubscribeLink(RecipientsModel recipient)
    {
        var data = new object[] { recipient.Id.ToString(), recipient.LinkToken, "3901", "c66156f17132", false };
        return CreateLink(data);
    }


    private string CreateLink(object[] data)
    {
        var serializedData = JsonSerializer.Serialize(data);
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(serializedData));

        return BaseUrl + Uri.EscapeDataString(base64Data);
    }
}
