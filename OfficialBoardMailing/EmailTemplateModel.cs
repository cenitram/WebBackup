namespace OfficialBoardMailing;
public class EmailTemplateModel
{
    public DateTime Date { get; set; }
    public IEnumerable<OfficialBoardModel> BoardDocuments { get; set; } = [];
    public string UnsubscribeLink { get; set; }
    public string SubscriptionManagementLink { get; set; }
}
