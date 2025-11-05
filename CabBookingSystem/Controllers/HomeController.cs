using Microsoft.AspNetCore.Mvc;
using CabBookingSystem.Models;
using CabBookingSystem.ViewModels;
using CabBookingSystem.Repositories;
using Microsoft.AspNetCore.Http;

namespace CabBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBookingRepository _bookingRepository;

        public HomeController(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                // User is not logged in - show welcome page
                return RedirectToAction("Welcome");
            }

            // User is logged in - show dashboard
            return RedirectToAction("Dashboard");
        }

        public IActionResult Welcome()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userId))
            {
                // If user is already logged in, go to dashboard
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Welcome");
            }

            // Get user's bookings from repository
            var bookingRepository = HttpContext.RequestServices.GetService<IBookingRepository>();
            var userBookings = await bookingRepository.GetUserBookingsAsync(userId);

            // Filter out cancelled bookings for statistics
            var activeBookings = userBookings.Where(b => b.Status != BookingStatus.Cancelled);

            // Filter for this month's bookings
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var thisMonthBookings = activeBookings.Where(b =>
                b.BookingTime.Month == currentMonth &&
                b.BookingTime.Year == currentYear
            );

            // Calculate real statistics (excluding cancelled bookings)
            var dashboardData = new DashboardViewModel
            {
                TotalRides = thisMonthBookings.Count(),
                TotalSpent = thisMonthBookings.Sum(b => b.Fare),
                RecentBookings = activeBookings.Take(5).ToList(), // Show last 5 active bookings
                AllBookings = activeBookings.ToList() // Add all active bookings for "View All" link
            };

            return View(dashboardData);
        }

        public IActionResult BookRide()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}