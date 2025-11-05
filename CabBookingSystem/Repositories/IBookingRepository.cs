using CabBookingSystem.Models;

namespace CabBookingSystem.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId);
        Task<IEnumerable<Booking>> GetAvailableBookingsAsync();
        Task<Booking> GetByIdAsync(string id);
        Task UpdateAsync(Booking booking);
        Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status);
        Task<IEnumerable<Booking>> GetRecentBookingsAsync(int count = 5);

        // NEW METHOD ADDED:
        Task<IEnumerable<Booking>> GetBookingsWithDetailsAsync();
    }
}