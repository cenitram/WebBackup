namespace OfficialBoardMailing;

public interface IMailPoetLinkService
{
    string CreateUnsubscribeLink(RecipientsModel recipient);
}