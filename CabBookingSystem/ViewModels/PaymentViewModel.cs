using System.ComponentModel.DataAnnotations;

namespace CabBookingSystem.ViewModels
{
    public class PaymentViewModel
    {
        [Required]
        public string BookingId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; }

        // Credit/Debit Card Fields
        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Card Holder Name")]
        public string? CardHolderName { get; set; }

        [Display(Name = "Expiry Date")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "CVV")]
        public string? CVV { get; set; }

        // UPI Fields
        [Display(Name = "UPI ID")]
        public string? UpiId { get; set; }

        // Wallet Fields
        [Display(Name = "Wallet Type")]
        public string? WalletType { get; set; }
    }
}