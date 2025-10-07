namespace Tlumacov.OfficialBoard.Shared.Models;

public class EmailTemplateModel
{
    public DateTime Date { get; set; }
    public IEnumerable<OfficialBoardModel> BoardDocuments { get; set; } = [];
    public required string UnsubscribeLink { get; set; }
}
