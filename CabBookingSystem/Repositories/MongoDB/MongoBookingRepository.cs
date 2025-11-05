using MongoDB.Driver;
using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace CabBookingSystem.Repositories.MongoDB
{
    public class MongoBookingRepository : IBookingRepository
    {
        private readonly IMongoCollection<Booking> _bookings;
        private readonly ICabRepository _cabRepository;
        private readonly IUserRepository _userRepository;

        // Updated constructor with UserRepository
        public MongoBookingRepository(IOptions<MongoDBSettings> settings, ICabRepository cabRepository, IUserRepository userRepository)
        {
            if (string.IsNullOrEmpty(settings.Value.ConnectionString))
            {
                throw new ArgumentException("MongoDB connection string is not configured");
            }

            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _bookings = database.GetCollection<Booking>("Bookings");
            _cabRepository = cabRepository;
            _userRepository = userRepository;
        }

        public async Task<Booking> GetByIdAsync(string id)
        {
            var booking = await _bookings.Find(b => b.Id == id).FirstOrDefaultAsync();
            if (booking != null)
            {
                await LoadRelatedData(booking);
            }
            return booking;
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            var bookings = await _bookings.Find(_ => true).ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        public async Task<IEnumerable<Booking>> FindAsync(Expression<Func<Booking, bool>> predicate)
        {
            var bookings = await _bookings.Find(predicate).ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        public async Task AddAsync(Booking entity)
        {
            await _bookings.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(Booking entity)
        {
            await _bookings.ReplaceOneAsync(b => b.Id == entity.Id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _bookings.DeleteOneAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
        {
            var bookings = await _bookings.Find(b => b.UserId == userId)
                                         .SortByDescending(b => b.BookingTime)
                                         .ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        public async Task<IEnumerable<Booking>> GetAvailableBookingsAsync()
        {
            var bookings = await _bookings.Find(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed).ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(BookingStatus status)
        {
            var bookings = await _bookings.Find(b => b.Status == status)
                                         .SortByDescending(b => b.BookingTime)
                                         .ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        public async Task<IEnumerable<Booking>> GetRecentBookingsAsync(int count = 5)
        {
            var bookings = await _bookings.Find(_ => true)
                                         .SortByDescending(b => b.BookingTime)
                                         .Limit(count)
                                         .ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        // NEW: Get bookings with cab and user details
        public async Task<IEnumerable<Booking>> GetBookingsWithDetailsAsync()
        {
            var bookings = await _bookings.Find(_ => true)
                                         .SortByDescending(b => b.BookingTime)
                                         .ToListAsync();
            await LoadRelatedData(bookings);
            return bookings;
        }

        // NEW: Load related data for a single booking
        private async Task LoadRelatedData(Booking booking)
        {
            if (booking != null)
            {
                // Load Cab details
                if (!string.IsNullOrEmpty(booking.CabId) && booking.Cab == null)
                {
                    booking.Cab = await _cabRepository.GetByIdAsync(booking.CabId);
                }

                // Load User details
                if (!string.IsNullOrEmpty(booking.UserId) && booking.User == null)
                {
                    booking.User = await _userRepository.GetByIdAsync(booking.UserId);
                }
            }
        }

        // NEW: Load related data for multiple bookings
        private async Task LoadRelatedData(List<Booking> bookings)
        {
            foreach (var booking in bookings)
            {
                await LoadRelatedData(booking);
            }
        }
    }
}