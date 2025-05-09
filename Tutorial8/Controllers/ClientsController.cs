using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientsService _clientsService;

        public ClientsController(IClientsService clientsService)
        {
            _clientsService = clientsService;
        }

        
        /* POST /api/clients
           creates a new client based on request body
           returns 201 Created with new client ID, or 400 if invalid */
        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO client)
        {
            if (!ModelState.IsValid) return BadRequest();
            
            try 
            {
                int id = await _clientsService.CreateClient(client);
                return CreatedAtAction(nameof(CreateClient), new { id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /* DELETE /api/clients/{idClient}/trips/{idTrip}
           removes a client’s registration from a specific trip
           returns 204 No Content or 404 if registration not found */
        [HttpDelete("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> DeleteTripRegistration(int idClient, int idTrip)
        {
            try 
            {
                await _clientsService.DeleteTripRegistration(idClient, idTrip);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        
        /* GET /api/clients/{idClient}/trips
           returns all trips a client is registered for
           returns 200 OK with trip list, or 404 if client not found */
        [HttpGet("{idClient}/trips")]
        public async Task<IActionResult> GetClientTrips(int idClient)
        {
            try 
            {
                var trips = await _clientsService.GetClientTrips(idClient);
                return Ok(trips);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /* PUT /api/clients/{idClient}/trips/{idTrip}
           registers the client for a trip if not full
           returns 200 OK or 404 Not Found */
        [HttpPut("{idClient}/trips/{idTrip}")]
        public async Task<IActionResult> AssignToTrip(int idClient, int idTrip)
        {
            try 
            {
                await _clientsService.AssignToTrip(idClient, idTrip);
                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}