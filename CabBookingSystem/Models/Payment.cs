using System.ComponentModel.DataAnnotations;

namespace CabBookingSystem.Models
{
    public class Payment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string BookingId { get; set; }
        public Booking Booking { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; } // Added navigation property

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        public string? PaymentDetails { get; set; }
    }

    public enum PaymentMethod
    {
        CreditCard,
        DebitCard,
        UPI,
        Wallet,
        Cash
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }
}