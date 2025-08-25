namespace OfficialBoardMailing.Repositories;

public interface IRecipientsRepository
{
    IEnumerable<string> GetRecipientsEmails();
}
