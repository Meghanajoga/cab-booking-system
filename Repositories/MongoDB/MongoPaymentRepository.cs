using MongoDB.Driver;
using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace CabBookingSystem.Repositories.MongoDB
{
    public class MongoPaymentRepository : IPaymentRepository
    {
        private readonly IMongoCollection<Payment> _payments;
        private readonly IUserRepository _userRepository;
        private readonly IBookingRepository _bookingRepository;

        // Updated constructor with UserRepository and BookingRepository
        public MongoPaymentRepository(IOptions<MongoDBSettings> settings, IUserRepository userRepository, IBookingRepository bookingRepository)
        {
            if (string.IsNullOrEmpty(settings.Value.ConnectionString))
            {
                throw new ArgumentException("MongoDB connection string is not configured");
            }

            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _payments = database.GetCollection<Payment>("Payments");
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<Payment> GetByIdAsync(string id)
        {
            var payment = await _payments.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (payment != null)
            {
                await LoadRelatedData(payment);
            }
            return payment;
        }

        public async Task<IEnumerable<Payment>> GetAllAsync()
        {
            var payments = await _payments.Find(_ => true).ToListAsync();
            await LoadRelatedData(payments);
            return payments;
        }

        public async Task<IEnumerable<Payment>> FindAsync(Expression<Func<Payment, bool>> predicate)
        {
            var payments = await _payments.Find(predicate).ToListAsync();
            await LoadRelatedData(payments);
            return payments;
        }

        public async Task AddAsync(Payment entity)
        {
            await _payments.InsertOneAsync(entity);
        }

        public async Task UpdateAsync(Payment entity)
        {
            await _payments.ReplaceOneAsync(p => p.Id == entity.Id, entity);
        }

        public async Task DeleteAsync(string id)
        {
            await _payments.DeleteOneAsync(p => p.Id == id);
        }

        public async Task<Payment> GetPaymentByBookingIdAsync(string bookingId)
        {
            var payment = await _payments.Find(p => p.BookingId == bookingId).FirstOrDefaultAsync();
            if (payment != null)
            {
                await LoadRelatedData(payment);
            }
            return payment;
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId)
        {
            var payments = await _payments.Find(p => p.UserId == userId)
                                 .SortByDescending(p => p.PaymentDate)
                                 .ToListAsync();
            await LoadRelatedData(payments);
            return payments;
        }

        // NEW: Get payments with user and booking details
        public async Task<IEnumerable<Payment>> GetPaymentsWithDetailsAsync()
        {
            var payments = await _payments.Find(_ => true)
                                         .SortByDescending(p => p.PaymentDate)
                                         .ToListAsync();
            await LoadRelatedData(payments);
            return payments;
        }

        public async Task<bool> ProcessPaymentAsync(Payment payment)
        {
            // Don't process cash payments here - they're handled in controller
            if (payment.Method == PaymentMethod.Cash)
            {
                return true;
            }

            // Simulate digital payment processing
            await Task.Delay(1000);

            var random = new Random();
            var isSuccess = random.Next(100) < 90;

            if (isSuccess)
            {
                payment.Status = PaymentStatus.Completed;
                payment.TransactionId = $"TXN{DateTime.Now:yyyyMMddHHmmss}";
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
            }

            await UpdateAsync(payment);
            return isSuccess;
        }

        // NEW: Load related data for a single payment
        private async Task LoadRelatedData(Payment payment)
        {
            if (payment != null)
            {
                // Load User details
                if (!string.IsNullOrEmpty(payment.UserId) && payment.User == null)
                {
                    payment.User = await _userRepository.GetByIdAsync(payment.UserId);
                }

                // Load Booking details
                if (!string.IsNullOrEmpty(payment.BookingId) && payment.Booking == null)
                {
                    payment.Booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
                }
            }
        }

        // NEW: Load related data for multiple payments
        private async Task LoadRelatedData(List<Payment> payments)
        {
            foreach (var payment in payments)
            {
                await LoadRelatedData(payment);
            }
        }
    }
}