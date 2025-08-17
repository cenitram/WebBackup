using MySqlConnector;

namespace OfficialBoardMailing;

public class OfficialBoardRepository : IOfficialBoardRepository
{
    private readonly MySqlConnection connection;
    private readonly string _query = @"
        SELECT udemail.id, d.id as dokument_id, d.nazev, d.popis, d.datum_vyveseni, s.nazev, s.pripona FROM wp_uredni_deska_dokumenty as d 
        JOIN wp_uredni_deska_soubory as s on s.dokument_id = d.id
        JOIN wp_uredni_deska_emails_sent as udemail on udemail.dokument_id = d.id
        WHERE udemail.email_sent = false;";

    public OfficialBoardRepository(MySqlConnection connection)
    {
        this.connection = connection;
    }

    public IEnumerable<OfficialBoardModel> GetUnsentDocuments()
    {
        if (connection == null)
            throw new InvalidOperationException("Not connected to database.");

        connection.Open();
        var list = new List<OfficialBoardModel>();
        using var cmd = new MySqlCommand(_query, connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new OfficialBoardModel
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                DokumentId = reader.GetInt32(reader.GetOrdinal("dokument_id")),
                Name = reader.GetString(reader.GetOrdinal("nazev")),
                Description = reader.IsDBNull(reader.GetOrdinal("popis")) ? null : reader.GetString(reader.GetOrdinal("popis")),
                DateOfPosting = reader.GetDateTime(reader.GetOrdinal("datum_vyveseni")),
                FileName = reader.GetString(reader.GetOrdinal("nazev")),
                FileExtension = reader.GetString(reader.GetOrdinal("pripona"))
            });
        }
        return list;
    }

    public void MarkDocumentsAsSent(IEnumerable<int> ids)
    {
        if (connection == null)
            throw new InvalidOperationException("Not connected to database.");

        if (ids == null || !ids.Any())
            return;

        connection.Open();

        // Prepare a parameterized query for multiple IDs
        var idList = ids.ToList();
        var parameters = string.Join(", ", idList.Select((id, idx) => $"@id{idx}"));
        var query = $"UPDATE wp_uredni_deska_emails_sent SET email_sent = true WHERE id IN ({parameters});";

        using var cmd = new MySqlCommand(query, connection);
        for (int i = 0; i < idList.Count; i++)
        {
            cmd.Parameters.AddWithValue($"@id{i}", idList[i]);
        }
        cmd.ExecuteNonQuery();
    }



}
