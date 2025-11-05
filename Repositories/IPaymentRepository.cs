using CabBookingSystem.Models;

namespace CabBookingSystem.Repositories
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<Payment> GetPaymentByBookingIdAsync(string bookingId);
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(string userId);
        Task<bool> ProcessPaymentAsync(Payment payment);

        // NEW METHOD ADDED:
        Task<IEnumerable<Payment>> GetPaymentsWithDetailsAsync();
    }
}