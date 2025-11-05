using CabBookingSystem.Models;

namespace CabBookingSystem.Repositories
{
    public interface ICabRepository : IRepository<Cab>
    {
        Task UpdateCabAvailabilityAsync(string cabId, bool isAvailable);
        Task<IEnumerable<Cab>> GetAvailableCabsAsync();
        Task<IEnumerable<Cab>> GetCabsByTypeAsync(string cabType);
        Task<Cab> GetByIdAsync(string id);
        Task<Cab> GetAvailableCabOrCreateNewAsync(string cabType);
    }
}