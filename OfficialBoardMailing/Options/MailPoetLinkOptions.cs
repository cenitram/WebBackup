namespace OfficialBoardMailing.Options;

public class MailPoetLinkOptions
{
    public required string QueueId { get; set; }
    public required string ManageSubscriptionHash { get; set; }
    public required string UnsubscribeHash { get; set; }
}
