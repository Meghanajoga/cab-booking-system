using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using CabBookingSystem.ViewModels;

namespace CabBookingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;

        // Updated constructor with UserRepository
        public PaymentController(IPaymentRepository paymentRepository, IBookingRepository bookingRepository, IUserRepository userRepository)
        {
            _paymentRepository = paymentRepository;
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Payment(string bookingId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var booking = await _bookingRepository.GetByIdAsync(bookingId);
            if (booking == null || booking.UserId != userId)
            {
                return NotFound();
            }

            // Get user details
            var user = await _userRepository.GetByIdAsync(userId);

            // Check if payment already exists
            var existingPayment = await _paymentRepository.GetPaymentByBookingIdAsync(bookingId);
            if (existingPayment != null && existingPayment.Status == PaymentStatus.Completed)
            {
                return RedirectToAction("PaymentSuccess", new { paymentId = existingPayment.Id });
            }

            var viewModel = new PaymentViewModel
            {
                BookingId = bookingId,
                Amount = booking.Fare
            };

            // Pass user and booking details to view
            ViewBag.UserName = $"{user?.FirstName} {user?.LastName}";
            ViewBag.BookingDetails = booking;

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var booking = await _bookingRepository.GetByIdAsync(model.BookingId);
                if (booking == null || booking.UserId != userId)
                {
                    return NotFound();
                }

                // Get user details
                var user = await _userRepository.GetByIdAsync(userId);

                // Create payment record with relationships
                var payment = new Payment
                {
                    BookingId = model.BookingId,
                    Booking = booking, // Set the booking object directly
                    UserId = userId,
                    User = user, // Set the user object directly
                    Amount = model.Amount,
                    Method = Enum.Parse<PaymentMethod>(model.PaymentMethod),
                    PaymentDetails = GetPaymentDetails(model)
                };

                await _paymentRepository.AddAsync(payment);

                // Process payment based on method
                bool isSuccess;

                if (payment.Method == PaymentMethod.Cash)
                {
                    // Cash payments are always successful immediately
                    payment.Status = PaymentStatus.Completed;
                    payment.TransactionId = null; // No transaction ID for cash
                    isSuccess = true;
                    await _paymentRepository.UpdateAsync(payment);
                }
                else
                {
                    // Process digital payments
                    isSuccess = await _paymentRepository.ProcessPaymentAsync(payment);
                }

                if (isSuccess)
                {
                    // Update booking status
                    booking.Status = BookingStatus.Confirmed;
                    await _bookingRepository.UpdateAsync(booking);

                    return RedirectToAction("PaymentSuccess", new { paymentId = payment.Id });
                }
                else
                {
                    return RedirectToAction("PaymentFailed", new { paymentId = payment.Id });
                }
            }

            return View("Payment", model);
        }

        public async Task<IActionResult> PaymentSuccess(string paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound();
            }

            // With the updated repository, booking and user details are automatically loaded
            return View(payment);
        }

        public async Task<IActionResult> PaymentFailed(string paymentId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // NEW: Get user payment history
        public async Task<IActionResult> MyPayments()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var payments = await _paymentRepository.GetUserPaymentsAsync(userId);

            // Get user details for display
            var user = await _userRepository.GetByIdAsync(userId);
            ViewBag.UserName = $"{user?.FirstName} {user?.LastName}";

            return View(payments);
        }

        // NEW: Get payment details with all relationships
        public async Task<IActionResult> PaymentDetails(string id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null) return NotFound();

            var userId = HttpContext.Session.GetString("UserId");
            if (payment.UserId != userId) return Forbid();

            return View(payment);
        }

        // NEW: Get all payments with details (for admin view)
        public async Task<IActionResult> AllPayments()
        {
            var payments = await _paymentRepository.GetPaymentsWithDetailsAsync();
            return View(payments);
        }

        private string GetPaymentDetails(PaymentViewModel model)
        {
            return model.PaymentMethod switch
            {
                "CreditCard" or "DebitCard" => $"Card: {model.CardNumber?.Substring(Math.Max(0, model.CardNumber?.Length - 4 ?? 0))}",
                "UPI" => $"UPI: {model.UpiId}",
                "Wallet" => $"Wallet: {model.WalletType}",
                "Cash" => "Cash Payment",
                _ => "Unknown"
            };
        }
    }
}