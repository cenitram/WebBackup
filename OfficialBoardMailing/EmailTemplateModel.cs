namespace OfficialBoardMailing;
public class EmailTemplateModel
{
    public DateTime Date { get; set; }
    public IEnumerable<OfficialBoardModel> BoardDocuments { get; set; } = [];
    public required string UnsubscribeLink { get; set; }
    public required string SubscriptionManagementLink { get; set; }
}
