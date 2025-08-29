namespace OfficialBoardMailing.Repositories;

public interface IRecipientsRepository
{
    IEnumerable<RecipientsModel> GetRecipientsEmails();
}
