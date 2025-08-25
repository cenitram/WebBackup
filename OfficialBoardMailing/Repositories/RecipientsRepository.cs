using MySqlConnector;

namespace OfficialBoardMailing.Repositories;

public class RecipientsRepository(MySqlConnection connection) : IRecipientsRepository
{
    private readonly MySqlConnection connection = connection;

    public IEnumerable<string> GetRecipientsEmails()
    {
        if (connection == null)
            throw new InvalidOperationException("Not connected to database.");

        connection.Open();
        var emails = new List<string>();
        const string query = @"SELECT email FROM `wp_mailpoet_subscribers` as s 
                JOIN `wp_mailpoet_subscriber_segment` as ss ON s.id = ss.subscriber_id 
                WHERE s.status IN ('subscribed', 'bounced', 'inactive') AND ss.segment_id = 4;";

        using var cmd = new MySqlCommand(query, connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            emails.Add(reader.GetString(reader.GetOrdinal("email")).Trim());
        }

        return emails;
    }
}
