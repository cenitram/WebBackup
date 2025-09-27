namespace OfficialBoardMailing.Options;

public class EmailOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;

    public string TokenSecret { get; set; } = string.Empty;
}
