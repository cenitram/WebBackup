using Tlumacov.OfficialBoard.Shared.Models;

namespace Tlumacov.OfficialBoard.Shared;

public interface IMailPoetLinkService
{
    string CreateUnsubscribeLink(RecipientsModel recipient);
}