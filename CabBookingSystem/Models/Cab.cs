namespace CabBookingSystem.Models
{
    public enum CabType
    {
        Mini,
        Sedan,
        SUV,
        Luxury
    }

    public class Cab
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public CabType Type { get; set; }
        public bool IsAvailable { get; set; } = true;

        // Navigation property
        public ICollection<Booking> Bookings { get; set; }
    }
}