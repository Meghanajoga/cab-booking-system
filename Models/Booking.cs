namespace CabBookingSystem.Models
{
    public class Booking
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PickupLocation { get; set; }
        public string DropoffLocation { get; set; }
        public string PickupLatitude { get; set; }  // Should be string
        public string PickupLongitude { get; set; } // Should be string
        public string DropoffLatitude { get; set; } // Should be string
        public string DropoffLongitude { get; set; } // Should be string
        public string CabId { get; set; }
        public Cab Cab { get; set; }
        public string CabType { get; set; }
        public double Distance { get; set; }
        public decimal Fare { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime BookingTime { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; } // Added navigation property

        // Navigation property for Payments
        public ICollection<Payment> Payments { get; set; }
    }

    public enum BookingStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Completed,
        Cancelled
    }
}