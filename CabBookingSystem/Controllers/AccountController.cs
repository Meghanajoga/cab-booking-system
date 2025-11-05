using Microsoft.AspNetCore.Mvc;
using CabBookingSystem.Models;
using CabBookingSystem.ViewModels;
using CabBookingSystem.Repositories;

namespace CabBookingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserRepository _userRepository;

        public AccountController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userRepository.GetUserByEmailAsync(model.Email);
                if (user != null && user.PasswordHash == model.Password)
                {
                    // Set session
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("FirstName", user.FirstName);
                    HttpContext.Session.SetString("Email", user.Email);

                    if (model.RememberMe)
                    {
                        var options = new CookieOptions { Expires = DateTime.Now.AddDays(30) };
                        Response.Cookies.Append("RememberMe", user.Id, options);
                    }

                    return RedirectToAction("Dashboard", "Home");
                }

                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during login.");
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if user already exists
                var existingUser = await _userRepository.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "User with this email already exists.");
                    return View(model);
                }

                // Create new user
                var user = new ApplicationUser // or User, depending on your model
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    PasswordHash = model.Password // In real app, hash this password
                };

                await _userRepository.AddAsync(user);

                // Auto-login after registration
                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetString("FirstName", user.FirstName);
                HttpContext.Session.SetString("Email", user.Email);

                return RedirectToAction("Dashboard", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        public IActionResult Logout()
        {
            // Clear session
            HttpContext.Session.Clear();

            // Clear remember me cookie
            Response.Cookies.Delete("RememberMe");

            return RedirectToAction("Welcome", "Home");
        }
    }
}