using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<TripDTO>> GetTrips();
    Task<bool> DoesTripExist(int tripId);
    Task<TripDTO> GetTripDetails(int tripId);
}