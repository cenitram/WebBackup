using MySqlConnector;

namespace OfficialBoardMailing.Repositories;

public class RecipientsRepository(MySqlConnection connection) : IRecipientsRepository
{
    private readonly MySqlConnection connection = connection;

    public IEnumerable<RecipientsModel> GetRecipientsEmails()
    {
        if (connection == null)
            throw new InvalidOperationException("Not connected to database.");

        connection.Open();
        var emails = new List<RecipientsModel>();
        const string query = @"SELECT Id, unsubscribe_token, email FROM `wp_uredni_deska_subscribers` 
                WHERE status IN ('subscribed', 'bounced', 'inactive');";

        using var cmd = new MySqlCommand(query, connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            emails.Add(new RecipientsModel(
                reader.GetInt32(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("unsubscribe_token")).Trim(),
                reader.GetString(reader.GetOrdinal("email")).Trim()
            ));
        }

        connection.Close();

        return emails;
    }

    public bool AddRecipient(string email)
    {
        if (connection == null)
            throw new InvalidOperationException("Not connected to database.");

        connection.Open();
        try
        {
            // Single atomic operation using ON DUPLICATE KEY UPDATE
            const string upsertQuery = @"
                INSERT INTO `wp_uredni_deska_subscribers` 
                (email, status, unsubscribe_token, subscribed_date) 
                VALUES (@Email, 'unconfirmed', @Token, NOW())
                ON DUPLICATE KEY UPDATE 
                email = email"; // No-op update to detect existing record

            string unsubscribeToken = Guid.NewGuid().ToString();

            using var cmd = new MySqlCommand(upsertQuery, connection);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Token", unsubscribeToken);

            int affectedRows = cmd.ExecuteNonQuery();

            // 1 = new row inserted, 2 = existing row "updated" (duplicate found)
            return affectedRows == 1;
        }
        finally
        {
            connection.Close();
        }
    }
}
