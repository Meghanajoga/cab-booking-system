using CabBookingSystem.Models;

namespace CabBookingSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalRides { get; set; }
        public decimal TotalSpent { get; set; }
        public IEnumerable<Booking> RecentBookings { get; set; } = new List<Booking>();
        public IEnumerable<Booking> AllBookings { get; set; } = new List<Booking>(); // Add this line
    }
}