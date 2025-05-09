using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<int> CreateClient(ClientCreateDTO client);
    Task DeleteTripRegistration(int clientId, int tripId);
    Task<List<ClientTripDTO>> GetClientTrips(int clientId);
    Task AssignToTrip(int clientId, int tripId);
}