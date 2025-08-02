namespace OfficialBoardMailing;

public interface IOfficialBoardRepository
{
    IEnumerable<OfficialBoardModel> GetUnsentDocuments();
    void MarkDocumentsAsSent(IEnumerable<int> ids);
}
