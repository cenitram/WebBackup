namespace OfficialBoardMailing;

public interface IEmailSender
{
    Task SendUnsentDocumentsAsync(IEnumerable<OfficialBoardModel> documents, CancellationToken cancellationToken = default);
}
