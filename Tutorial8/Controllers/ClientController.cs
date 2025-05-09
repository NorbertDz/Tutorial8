using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllersmaster;

[Route("api/[controller]")]
[ApiController]
public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientController(IClientService clientService)
    {
        _clientService = clientService;
    }
    
    [HttpGet("/api/clients/{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var trips = await _clientService.GetTripsForClient(id);
        if (trips == null || trips.Count == 0)
            return NotFound($"Client {id} not found or has no trips.");

        return Ok(trips);
    }

    [HttpPost("/api/clients")]
    public async Task<IActionResult> addNewClient([FromBody] ClientDTO client)
    {
        if (string.IsNullOrEmpty(client.FirstName) || string.IsNullOrEmpty(client.LastName))
        {
            return BadRequest("Name and Surname are required.");
        }

        await _clientService.addNewClient(client);

        return CreatedAtAction(nameof(GetClientTrips), new { id = client.Id }, client);
    }

    
    [HttpPut("/api/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> registerClientNewTrip(int id, int tripId)
    {
        try
        {
            await _clientService.registerClientNewTrip(id, tripId);
            return Ok($"Client {id} registered for trip {tripId}.");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpDelete("/api/clients/{id}/trips/{tripId}")]
    public async Task<IActionResult> UnregisterClientFromTrip(int id, int tripId)
    {
        var result = await _clientService.unregisterClientFromTrip(id, tripId);

        if (!result)
            return NotFound($"Registration for client {id} and trip {tripId} does not exist.");

        return Ok($"Client {id} has been unregistered from trip {tripId}.");
    }
}