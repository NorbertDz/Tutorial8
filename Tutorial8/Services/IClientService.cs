using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientService
{
    Task<List<ClientTripDTO>> GetTripsForClient(int clientId);

    Task addNewClient(ClientDTO client);
    
    Task registerClientNewTrip(int clientId, int tripId);
}