namespace OfficialBoardMailing;

public interface IMailPoetLinkService
{
    string CreateUnsubscribeLink(RecipientsModel recipient);
    string CreateManageSubscriptionLink(RecipientsModel recipient);
}