using Tlumacov.OfficialBoard.Shared.Models;

namespace Tlumacov.OfficialBoard.Shared.Repositories;

public interface IRecipientsRepository
{
    IEnumerable<RecipientsModel> GetRecipientsEmails();
    bool AddRecipient(string email);
    bool ConfirmSubscription(string email);
    bool UnsubscribeRecipient(string email);
}
