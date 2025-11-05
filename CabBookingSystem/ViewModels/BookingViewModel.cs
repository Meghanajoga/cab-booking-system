using System.ComponentModel.DataAnnotations;
using CabBookingSystem.Models;

namespace CabBookingSystem.ViewModels
{
    public class BookingViewModel
    {
        [Required]
        public string PickupLocation { get; set; }

        [Required]
        public string DropoffLocation { get; set; }

        [Required]
        public CabType CabType { get; set; }

        public string PickupLatitude { get; set; }
        public string PickupLongitude { get; set; }
        public string DropoffLatitude { get; set; }
        public string DropoffLongitude { get; set; }
    }
}