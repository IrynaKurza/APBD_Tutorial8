using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        /* GET /api/trips
           returns a list of all available trips with country info
           returns 200 OK */
        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _tripsService.GetTrips();
            return Ok(trips);
        }

        
        /* GET /api/trips/{id}
           returns detailed info about a specific trip
           returns 200 OK or 404 if trip doesn’t exist */
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            if (!await _tripsService.DoesTripExist(id))
            {
                return NotFound();
            }
            var trip = await _tripsService.GetTripDetails(id);
            return Ok(trip);
        }
    }
}