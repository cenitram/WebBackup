namespace OfficialBoardMailing;

public interface IEmailSender
{
    Task SendUnsentDocumentsAsync(IEnumerable<OfficialBoardModel> documents, IEnumerable<string> recipients, CancellationToken cancellationToken = default);
}
