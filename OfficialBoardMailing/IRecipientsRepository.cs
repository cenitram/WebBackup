namespace OfficialBoardMailing;

public interface IRecipientsRepository
{
    IEnumerable<string> GetRecipientsEmails();
}
