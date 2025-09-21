namespace OfficialBoardMailing.Repositories;

public interface IRecipientsRepository
{
    IEnumerable<RecipientsModel> GetRecipientsEmails();
    bool AddRecipient(string email);
}
