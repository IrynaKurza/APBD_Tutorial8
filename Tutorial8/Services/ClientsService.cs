using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    private readonly string? _connectionString;

    public ClientsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("TravelAgencyDb");
    }
    public async Task<int> CreateClient(ClientCreateDTO client)
    {
        // insert client and return its ID (used for POST /api/clients)
        string command = @"
            INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
            OUTPUT INSERTED.IdClient
            VALUES (@fn, @ln, @email, @tel, @pesel)";
            
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@fn", client.FirstName);
            cmd.Parameters.AddWithValue("@ln", client.LastName);
            cmd.Parameters.AddWithValue("@email", client.Email);
            cmd.Parameters.AddWithValue("@tel", client.Telephone);
            cmd.Parameters.AddWithValue("@pesel", client.Pesel);

            await conn.OpenAsync();
            var result = await cmd.ExecuteScalarAsync();
            if (result is null)
                throw new InvalidOperationException("Failed to get the inserted ID");
            return Convert.ToInt32(result);
        }
    }

    public async Task DeleteTripRegistration(int clientId, int tripId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            
            // check if client is registered for the trip (used for DELETE /api/clients/{id}/trips/{id})
            string checkCommand = "SELECT 1 FROM Client_Trip WHERE IdClient = @cid AND IdTrip = @tid";
            
            using (SqlCommand checkCmd = new SqlCommand(checkCommand, conn))
            {
                checkCmd.Parameters.AddWithValue("@cid", clientId);
                checkCmd.Parameters.AddWithValue("@tid", tripId);
                
                if (await checkCmd.ExecuteScalarAsync() == null)
                    throw new KeyNotFoundException("Registration not found");
            }

            // delete the registration if it exists
            string deleteCommand = "DELETE FROM Client_Trip WHERE IdClient = @cid AND IdTrip = @tid";
            
            using (SqlCommand deleteCmd = new SqlCommand(deleteCommand, conn))
            {
                deleteCmd.Parameters.AddWithValue("@cid", clientId);
                deleteCmd.Parameters.AddWithValue("@tid", tripId);
                
                await deleteCmd.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<List<ClientTripDTO>> GetClientTrips(int clientId)
    {
        var trips = new List<ClientTripDTO>();
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            // check if client exists (used for GET /api/clients/{id}/trips)
            string checkCommand = "SELECT 1 FROM Client WHERE IdClient = @id";
            
            using (SqlCommand checkCmd = new SqlCommand(checkCommand, conn))
            {
                checkCmd.Parameters.AddWithValue("@id", clientId);
                await conn.OpenAsync();
                if (await checkCmd.ExecuteScalarAsync() == null)
                    throw new KeyNotFoundException("Client not found");
            }

            // get all trips that the client is registered for, including dates and payment info
            string command = @"
                SELECT t.Name, ct.RegisteredAt, ct.PaymentDate 
                FROM Client_Trip ct
                JOIN Trip t ON ct.IdTrip = t.IdTrip
                WHERE ct.IdClient = @id";
                
            using (SqlCommand cmd = new SqlCommand(command, conn))
            {
                cmd.Parameters.AddWithValue("@id", clientId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    int nameOrdinal = reader.GetOrdinal("Name");
                    int registeredAtOrdinal = reader.GetOrdinal("RegisteredAt");
                    int paymentDateOrdinal = reader.GetOrdinal("PaymentDate");
                    
                    while (await reader.ReadAsync())
                    {
                        string? paymentDate = null;
                        if (!reader.IsDBNull(paymentDateOrdinal))
                            paymentDate = ConvertIntToDateString(reader.GetInt32(paymentDateOrdinal));
                            
                        trips.Add(new ClientTripDTO
                        {
                            TripName = reader.GetString(nameOrdinal),
                            RegisteredAt = ConvertIntToDateString(reader.GetInt32(registeredAtOrdinal)),
                            PaymentDate = paymentDate
                        });
                    }
                }
            }
        }
        return trips;
    }

    public async Task AssignToTrip(int clientId, int tripId)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (SqlTransaction transaction = conn.BeginTransaction())
            {
                try
                {
                    // check if client exists (used for PUT /api/clients/{id}/trips/{id})
                    string clientCommand = "SELECT 1 FROM Client WHERE IdClient = @cid";

                    using (SqlCommand clientCmd = new SqlCommand(clientCommand, conn, transaction))
                    {
                        clientCmd.Parameters.AddWithValue("@cid", clientId);
                        if (await clientCmd.ExecuteScalarAsync() == null)
                            throw new KeyNotFoundException("Client not found");
                    }

                    // check if trip exists and get max number of people
                    string tripCommand = "SELECT MaxPeople FROM Trip WHERE IdTrip = @tid";

                    using (SqlCommand tripCmd = new SqlCommand(tripCommand, conn, transaction))
                    {
                        tripCmd.Parameters.AddWithValue("@tid", tripId);
                        var maxPeopleObj = await tripCmd.ExecuteScalarAsync();
                        if (maxPeopleObj == null)
                            throw new KeyNotFoundException("Trip not found");

                        var maxPeople = Convert.ToInt32(maxPeopleObj);

                        // count current number of registered participants for this trip
                        string currentCommand = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @tid";

                        using (SqlCommand currentCmd = new SqlCommand(currentCommand, conn, transaction))
                        {
                            currentCmd.Parameters.AddWithValue("@tid", tripId);
                            var result = await currentCmd.ExecuteScalarAsync();
                            if (result is null)
                                throw new InvalidOperationException("Failed to get trip registration count");
                            var current = Convert.ToInt32(result);

                            if (current >= maxPeople)
                                throw new InvalidOperationException("Trip is full");
                        }
                    }

                    // check if the client is already registered for this trip
                    string existsCommand = "SELECT 1 FROM Client_Trip WHERE IdClient = @cid AND IdTrip = @tid";

                    using (SqlCommand existsCmd = new SqlCommand(existsCommand, conn, transaction))
                    {
                        existsCmd.Parameters.AddWithValue("@cid", clientId);
                        existsCmd.Parameters.AddWithValue("@tid", tripId);
                        if (await existsCmd.ExecuteScalarAsync() != null)
                            throw new InvalidOperationException("Client already registered");
                    }

                    int todayAsInt = GetTodayAsInt();

                    // insert new registration with today's date
                    string insertCommand = @"
                INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt)
                VALUES (@cid, @tid, @date)";

                    using (SqlCommand insertCmd = new SqlCommand(insertCommand, conn, transaction))
                    {
                        insertCmd.Parameters.AddWithValue("@cid", clientId);
                        insertCmd.Parameters.AddWithValue("@tid", tripId);
                        insertCmd.Parameters.AddWithValue("@date", todayAsInt);
                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();
                }catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
    
    
    // helper methods
    private string ConvertIntToDateString(int dateInt)
    {
        string dateStr = dateInt.ToString();
        if (dateStr.Length != 8) return dateStr; 
        return $"{dateStr.Substring(0, 4)}-{dateStr.Substring(4, 2)}-{dateStr.Substring(6, 2)}";
    }
    
    private int GetTodayAsInt()
    {
        DateTime today = DateTime.Today;
        return today.Year * 10000 + today.Month * 100 + today.Day;
    }
    
}