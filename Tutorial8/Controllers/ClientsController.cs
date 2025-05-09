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