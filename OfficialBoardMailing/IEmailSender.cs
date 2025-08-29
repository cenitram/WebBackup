namespace OfficialBoardMailing;

public interface IEmailSender
{
    Task SendUnsentDocumentsAsync(IEnumerable<OfficialBoardModel> documents, IEnumerable<RecipientsModel> recipients, CancellationToken cancellationToken = default);
}
