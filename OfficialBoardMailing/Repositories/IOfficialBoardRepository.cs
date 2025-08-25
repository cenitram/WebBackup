namespace OfficialBoardMailing.Repositories;

public interface IOfficialBoardRepository
{
    IEnumerable<OfficialBoardModel> GetUnsentDocuments();
    void MarkDocumentsAsSent(IEnumerable<int> ids);
}
