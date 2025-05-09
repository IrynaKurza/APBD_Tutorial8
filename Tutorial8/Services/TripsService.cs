using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string? _connectionString;

    public TripsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TravelAgencyDb");
    }
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();
        
        // get all trips with country info (used for GET /api/trips)
        string command = @"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name as CountryName 
            FROM Trip t
            JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            JOIN Country c ON ct.IdCountry = c.IdCountry
            ORDER BY t.DateFrom DESC";
            
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int idTripOrdinal = reader.GetOrdinal("IdTrip");
                int nameOrdinal = reader.GetOrdinal("Name");
                int descriptionOrdinal = reader.GetOrdinal("Description");
                int dateFromOrdinal = reader.GetOrdinal("DateFrom");
                int dateToOrdinal = reader.GetOrdinal("DateTo");
                int maxPeopleOrdinal = reader.GetOrdinal("MaxPeople");
                int countryNameOrdinal = reader.GetOrdinal("CountryName");
                
                while (await reader.ReadAsync())
                {
                    var tripId = reader.GetInt32(idTripOrdinal);
                    var trip = trips.FirstOrDefault(t => t.Id == tripId);
                    
                    if (trip == null)
                    {
                        trip = new TripDTO
                        {
                            Id = tripId,
                            Name = reader.GetString(nameOrdinal),
                            Description = reader.GetString(descriptionOrdinal),
                            DateFrom = reader.GetDateTime(dateFromOrdinal),
                            DateTo = reader.GetDateTime(dateToOrdinal),
                            MaxPeople = reader.GetInt32(maxPeopleOrdinal)
                        };
                        trips.Add(trip);
                    }
                    
                    trip.Countries.Add(new CountryDTO { Name = reader.GetString(countryNameOrdinal) });
                }
            }
        }
        return trips;
    }

    public async Task<bool> DoesTripExist(int tripId)
    {
        // check if a trip exists (used for GET /api/trips/{id})
        string command = "SELECT 1 FROM Trip WHERE IdTrip = @id";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@id", tripId);
            await conn.OpenAsync();
            return await cmd.ExecuteScalarAsync() != null;
        }
    }

    public async Task<TripDTO> GetTripDetails(int tripId)
    {
        // get details of a specific trip with country list (used for GET /api/trips/{id})
        string command = @"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name as CountryName 
            FROM Trip t
            LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
            LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
            WHERE t.IdTrip = @id";
            
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@id", tripId);
            
            TripDTO? trip = null;
            await conn.OpenAsync();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                int idTripOrdinal = reader.GetOrdinal("IdTrip");
                int nameOrdinal = reader.GetOrdinal("Name");
                int descriptionOrdinal = reader.GetOrdinal("Description");
                int dateFromOrdinal = reader.GetOrdinal("DateFrom");
                int dateToOrdinal = reader.GetOrdinal("DateTo");
                int maxPeopleOrdinal = reader.GetOrdinal("MaxPeople");
                int countryNameOrdinal = reader.GetOrdinal("CountryName");
                
                while (await reader.ReadAsync())
                {
                    if (trip == null)
                    {
                        trip = new TripDTO
                        {
                            Id = reader.GetInt32(idTripOrdinal),
                            Name = reader.GetString(nameOrdinal),
                            Description = reader.GetString(descriptionOrdinal),
                            DateFrom = reader.GetDateTime(dateFromOrdinal),
                            DateTo = reader.GetDateTime(dateToOrdinal),
                            MaxPeople = reader.GetInt32(maxPeopleOrdinal)
                        };
                    }
                    
                    if (!reader.IsDBNull(countryNameOrdinal))
                    {
                        trip.Countries.Add(new CountryDTO { Name = reader.GetString(countryNameOrdinal) });
                    }
                }
            }
            return trip ?? throw new KeyNotFoundException("Trip not found");
        }
    }
}