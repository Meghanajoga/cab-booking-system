using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CabBookingSystem.Models;
using CabBookingSystem.ViewModels;
using CabBookingSystem.Repositories;

namespace CabBookingSystem.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ICabRepository _cabRepository;
        private readonly IUserRepository _userRepository;

        // Updated constructor with UserRepository
        public BookingController(IBookingRepository bookingRepository, ICabRepository cabRepository, IUserRepository userRepository)
        {
            _bookingRepository = bookingRepository;
            _cabRepository = cabRepository;
            _userRepository = userRepository;
        }

        [HttpPost]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Home/BookRide.cshtml", model);
            }

            try
            {
                // Convert CabType enum to string
                string cabTypeString = model.CabType.ToString();

                // USE THE NEW METHOD: Get available cab or create new one
                var availableCab = await _cabRepository.GetAvailableCabOrCreateNewAsync(cabTypeString);

                if (availableCab == null)
                {
                    ModelState.AddModelError("CabType", $"Unable to book {model.CabType} cab. Please try another type.");
                    return View("~/Views/Home/BookRide.cshtml", model);
                }

                // Calculate distance and fare
                double distance = CalculateDistance(
                    ParseDouble(model.PickupLatitude) ?? 0,
                    ParseDouble(model.PickupLongitude) ?? 0,
                    ParseDouble(model.DropoffLatitude) ?? 0,
                    ParseDouble(model.DropoffLongitude) ?? 0
                );

                decimal fare = CalculateFare(distance, cabTypeString);

                // Get current user ID
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get user details for the booking
                var user = await _userRepository.GetByIdAsync(userId);

                // Create the booking
                var booking = new Booking
                {
                    Id = Guid.NewGuid().ToString(),
                    PickupLocation = model.PickupLocation,
                    DropoffLocation = model.DropoffLocation,
                    PickupLatitude = model.PickupLatitude,
                    PickupLongitude = model.PickupLongitude,
                    DropoffLatitude = model.DropoffLatitude,
                    DropoffLongitude = model.DropoffLongitude,
                    CabId = availableCab.Id,
                    Cab = availableCab, // Set the cab object directly
                    CabType = cabTypeString,
                    Distance = distance,
                    Fare = fare,
                    Status = BookingStatus.Pending,
                    BookingTime = DateTime.UtcNow,
                    UserId = userId,
                    User = user // Set the user object directly
                };

                // Mark cab as unavailable
                await _cabRepository.UpdateCabAvailabilityAsync(availableCab.Id, false);

                // Save booking
                await _bookingRepository.AddAsync(booking);

                return RedirectToAction("BookingConfirmation", new { id = booking.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while creating your booking. Please try again.");
                return View("~/Views/Home/BookRide.cshtml", model);
            }
        }

        public async Task<IActionResult> BookingConfirmation(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();

            // With the updated repository, cab and user details are automatically loaded
            // So we don't need to manually load them anymore

            return View(booking);
        }

        public async Task<IActionResult> MyBookings()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var bookings = await _bookingRepository.GetUserBookingsAsync(userId);

            // Get user details for display
            var user = await _userRepository.GetByIdAsync(userId);
            ViewBag.UserName = $"{user?.FirstName} {user?.LastName}";

            return View(bookings);
        }

        // NEW: Get booking details with all relationships
        public async Task<IActionResult> BookingDetails(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();

            var userId = HttpContext.Session.GetString("UserId");
            if (booking.UserId != userId) return Forbid();

            return View(booking);
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(string id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var booking = await _bookingRepository.GetByIdAsync(id);

            if (booking == null || booking.UserId != userId) return NotFound();

            if (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
            {
                // Store booking details for the cancellation message
                TempData["CancellationMessage"] = $"Booking #{booking.Id} has been cancelled successfully. Refund of ₹{booking.Fare:F2} will be processed within 1-2 hours.";
                TempData["CancellationDetails"] = $"From: {booking.PickupLocation} | To: {booking.DropoffLocation}";

                // Update booking status
                booking.Status = BookingStatus.Cancelled;

                // Mark cab as available again
                if (!string.IsNullOrEmpty(booking.CabId))
                {
                    await _cabRepository.UpdateCabAvailabilityAsync(booking.CabId, true);
                }

                await _bookingRepository.UpdateAsync(booking);
                return RedirectToAction("Dashboard", "Home");
            }

            return RedirectToAction("Dashboard", "Home");
        }

        // NEW: Get all bookings with details (for admin view)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllBookings()
        {
            var bookings = await _bookingRepository.GetBookingsWithDetailsAsync();
            return View(bookings);
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var random = new Random();
            return random.Next(5, 20) + random.NextDouble();
        }

        private decimal CalculateFare(double distance, string cabType)
        {
            decimal baseFare = cabType switch
            {
                "Mini" => 25m,
                "Sedan" => 35m,
                "SUV" => 50m,
                "Luxury" => 80m,
                _ => 30m
            };
            decimal ratePerKm = cabType switch
            {
                "Mini" => 12m,
                "Sedan" => 15m,
                "SUV" => 20m,
                "Luxury" => 30m,
                _ => 13m
            };
            return baseFare + (decimal)(distance * (double)ratePerKm);
        }

        private double? ParseDouble(string value)
        {
            if (double.TryParse(value, out double result))
            {
                return result;
            }
            return null;
        }
    }
}