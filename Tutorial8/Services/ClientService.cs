using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientService : IClientService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;;MultipleActiveResultSets=True;";

public async Task<List<ClientTripDTO>> GetTripsForClient(int clientId)
    {
        var trips = new List<ClientTripDTO>();
        var tripDict = new Dictionary<int, ClientTripDTO>();

        string query = @"
        SELECT 
                t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                ct.RegisteredAt, ct.PaymentDate,
                c.Name AS CountryName
            FROM Client_Trip ct
            JOIN Trip t ON ct.IdTrip = t.IdTrip
            LEFT JOIN Country_Trip ctr ON t.IdTrip = ctr.IdTrip
            LEFT JOIN Country c ON c.IdCountry = ctr.IdCountry
            WHERE ct.IdClient = @IdClient
            ORDER BY t.IdTrip";

    using (var conn = new SqlConnection(_connectionString))
    using (var cmd = new SqlCommand(query, conn))
    {
        cmd.Parameters.AddWithValue("@IdClient", clientId);
        await conn.OpenAsync();

        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

                if (!tripDict.ContainsKey(tripId))
                {
                    var trip = new ClientTripDTO
                    {
                        Id = tripId,
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        Description = reader.GetString(reader.GetOrdinal("Description")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                        RegisteredAt = DateTime.ParseExact(reader.GetInt32(reader.GetOrdinal("RegisteredAt")).ToString(), "yyyyMMdd", null),
                        PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                            ? null
                            : DateTime.ParseExact(reader.GetInt32(reader.GetOrdinal("PaymentDate")).ToString(), "yyyyMMdd", null),
                        Countries = new List<CountryDTO>()
                    };

                    tripDict[tripId] = trip;
                    trips.Add(trip);
                }

                if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
                {
                    tripDict[tripId].Countries.Add(new CountryDTO
                    {
                        Name = reader.GetString(reader.GetOrdinal("CountryName"))
                    });
                }
            }
        }
    }

    return trips;
    }

    public Task addNewClient(ClientDTO client)
    {
        string query = "INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";

        using (var conn = new SqlConnection(_connectionString))
        using (var cmd = new SqlCommand(query, conn))
        {
            conn.Open();
            
            cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
            cmd.Parameters.AddWithValue("@LastName", client.LastName);
            cmd.Parameters.AddWithValue("@Email", client.Email);
            cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
            cmd.Parameters.AddWithValue("@Pesel", client.Pesel);
            
            cmd.ExecuteNonQuery();
        }
        return Task.CompletedTask;
    }

    public async Task registerClientNewTrip(int clientId, int tripId)
    {
        string checkClientQuery = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        string checkTripQuery = "SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip";
        string countParticipantsQuery = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip";
        string insertQuery = @"
        INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt,PaymentDate)
        VALUES (@IdClient, @IdTrip, @RegisteredAt,@PaymentDate)";

        using (var conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            using (var checkClientCmd = new SqlCommand(checkClientQuery, conn))
            {
                checkClientCmd.Parameters.AddWithValue("@IdClient", clientId);
                var exists = checkClientCmd.ExecuteScalarAsync();
                if (exists == null)
                    throw new Exception($"Client {clientId} does not exist");
            }

            int maxPeople;
            using (var checkTripCmd = new SqlCommand(checkTripQuery, conn))
            {
                checkTripCmd.Parameters.AddWithValue("@IdTrip", tripId);
                var result = await checkTripCmd.ExecuteScalarAsync();
                if (result == null)
                    throw new Exception($"Trip {tripId} does not exist");

                maxPeople = (int)result;
            }

            int currentPeople;
            using (var countCmd = new SqlCommand(countParticipantsQuery, conn))
            {
                countCmd.Parameters.AddWithValue("@IdTrip", tripId);
                currentPeople = (int)await countCmd.ExecuteScalarAsync();
            }

            if (currentPeople >= maxPeople)
                throw new Exception("Trip is full");

            using (var insertCmd = new SqlCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@IdClient", clientId);
                insertCmd.Parameters.AddWithValue("@IdTrip", tripId);
                insertCmd.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
                insertCmd.Parameters.AddWithValue("@PaymentDate", DateTime.Now.ToString("yyyyMMdd"));

                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<bool> unregisterClientFromTrip(int clientId, int tripId)
    {
        string checkQuery = @"
        SELECT 1 FROM Client_Trip 
        WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
        

        string deleteQuery = @"
        DELETE FROM Client_Trip 
        WHERE IdClient = @IdClient AND IdTrip = @IdTrip";

        using (var conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();

            using (var checkCmd = new SqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@IdClient", clientId);
                checkCmd.Parameters.AddWithValue("@IdTrip", tripId);

                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists == null)
                    return false;
            }

            using (var deleteCmd = new SqlCommand(deleteQuery, conn))
            {
                deleteCmd.Parameters.AddWithValue("@IdClient", clientId);
                deleteCmd.Parameters.AddWithValue("@IdTrip", tripId);

                await deleteCmd.ExecuteNonQueryAsync();
            }
        }

        return true;
    }
}