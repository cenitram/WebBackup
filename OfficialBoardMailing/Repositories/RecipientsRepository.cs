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
            // Check if already exists
            const string checkQuery = "SELECT COUNT(*) FROM `wp_uredni_deska_subscribers` WHERE email = @Email;";
            using (var checkCmd = new MySqlCommand(checkQuery, connection))
            {
                checkCmd.Parameters.AddWithValue("@Email", email);
                var result = checkCmd.ExecuteScalar();
                var count = Convert.ToInt32(result);
                if (count > 0)
                    return false;
            }

            // Insert new subscriber
            const string insertQuery = @"INSERT INTO `wp_uredni_deska_subscribers` (email, status, unsubscribe_token, subscribed_date) VALUES (@Email, 'unconfirmed', @Token, NOW());";
            string unsubscribeToken = Guid.NewGuid().ToString();

            using var insertCmd = new MySqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@Email", email);
            insertCmd.Parameters.AddWithValue("@Token", unsubscribeToken);
            insertCmd.ExecuteNonQuery();

            return true;
        }
        finally
        {
            connection.Close();
        }
    }
}
