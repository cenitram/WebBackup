namespace Tlumacov.OfficialBoard.Shared.Models;

public class OfficialBoardModel
{
    public int Id { get; set; }
    public int DokumentId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime DateOfPosting { get; set; }
    public required string FileName { get; set; }
    public required string FileExtension { get; set; }
}
