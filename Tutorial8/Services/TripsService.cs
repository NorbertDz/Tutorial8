using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;";

    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();
        var tripDict = new Dictionary<int, TripDTO>();

        string command =
            "SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,Country.Name FROM Trip as t left join Country_Trip on t.IdTrip = Country_Trip.IdTrip left join Country on Country.IdCountry = Country_Trip.IdCountry Order by t.IdTrip";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetOrdinal("IdTrip");

                    while (await reader.ReadAsync())
                    {
                        int tripId = reader.GetInt32(idOrdinal);

                        if (!tripDict.ContainsKey(tripId))
                        {
                            var trip = new TripDTO
                            {
                                Id = tripId,
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                                DateFrom = reader.GetDateTime(3),
                                DateTo = reader.GetDateTime(4),
                                MaxPeople = reader.GetInt32(5),
                                Countries = new List<CountryDTO>()
                            };

                            tripDict[tripId] = trip;
                            trips.Add(trip);
                        }

                        if (!reader.IsDBNull(6))
                        {
                            var countryName = reader.GetString(6);
                            tripDict[tripId].Countries.Add(new CountryDTO { Name = countryName });
                        }
                    }
                }
            }

            return trips;
        }
    }

    
}