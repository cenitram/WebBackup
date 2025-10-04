using Tlumacov.OfficialBoard.Shared.Models;

namespace Tlumacov.OfficialBoard.Shared;

public interface IEmailSender
{
    Task SendUnsentDocumentsAsync(IEnumerable<OfficialBoardModel> documents, IEnumerable<RecipientsModel> recipients, CancellationToken cancellationToken = default);

    // Sends a confirmation email to a single recipient (subject and body are handled by the implementation)
    Task SendConfirmationEmailAsync(string toEmail, CancellationToken cancellationToken = default);
}
